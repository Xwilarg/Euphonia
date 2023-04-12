package com.example.euphonia

import PlaybackService
import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.ComponentName
import android.content.Intent
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.ListView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.NotificationCompat
import androidx.core.content.getSystemService
import androidx.core.net.toUri
import androidx.media3.common.MediaItem
import androidx.media3.common.MediaMetadata
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.session.MediaController
import androidx.media3.session.SessionToken
import com.example.euphonia.data.MusicData
import com.example.euphonia.data.Song
import com.google.common.util.concurrent.MoreExecutors
import com.google.gson.Gson
import java.io.File
import java.io.FileOutputStream
import java.net.URL
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors


class MainActivity : AppCompatActivity() {
    // Current playlist that need to be displayed
    var currentPlaylist: String? = null

    // List of songs that were downloaded
    var downloaded: MutableList<Song> = mutableListOf()

    lateinit var list: ListView

    lateinit var data: MusicData

    override fun onBackPressed() {
        if (currentPlaylist != null) {
            currentPlaylist = null
            updateList()
        } else {
            onBackPressedDispatcher.onBackPressed()
        }
    }

    fun shouldDisplaySongs(): Boolean {
        return data.playlists == null || data.playlists!!.isEmpty() || currentPlaylist != null;
    }

    fun updateList() {
        val handler = Handler(Looper.getMainLooper())
        handler.post {
            var adapter =
                if (shouldDisplaySongs()) {
                    ArrayAdapter(this, android.R.layout.simple_list_item_1, downloaded.filter { currentPlaylist == null || it.playlist == currentPlaylist }.map { it.name })
                } else {
                    ArrayAdapter(this, android.R.layout.simple_list_item_1, data.playlists!!.map { it.value.name })
                }
            list.adapter = adapter
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        // Ensure remote server is init
        val sharedPref = this.getSharedPreferences("settings", MODE_PRIVATE)
        val url = sharedPref.getString("remoteServer", null)
        if (url == null) {
            val intent = Intent(applicationContext, SetupActivity::class.java)
            startActivity(intent)
        }

        //  Init media session
        val sessionToken = SessionToken(this, ComponentName(this, PlaybackService::class.java))
        val controllerFuture = MediaController.Builder(this, sessionToken).buildAsync()
        controllerFuture.addListener(
            {
                //player = controllerFuture.get().
                //playerView.player = controllerFuture.get()
            },
            MoreExecutors.directExecutor()
        )

        // Update JSON info
        val executor: ExecutorService = Executors.newSingleThreadExecutor()

        list = findViewById(R.id.musicData)

        val notificationManager = this.getSystemService<NotificationManager>()!!

        val builder = NotificationCompat.Builder(this, "download_channel")
            .setContentTitle("Updating data...")
            .setSmallIcon(R.drawable.icon)
            .setPriority(NotificationCompat.PRIORITY_DEFAULT)
            .setOngoing(true)
        val channel = NotificationChannel("download_channel", "Data Download Progress", NotificationManager.IMPORTANCE_LOW)
        notificationManager.createNotificationChannel(channel)
        builder.setChannelId("download_channel")
        notificationManager.notify(1, builder.build())

        downloaded = mutableListOf()

        if (!File(filesDir, "${url}info.json").exists()) {
            // Somehow the target file is missing? We need the user to go by the setup phase again
            val intent = Intent(applicationContext, SetupActivity::class.java)
            startActivity(intent)
            return
        }
        data = Gson().fromJson(File(filesDir, "${url}info.json").readText(), MusicData::class.java)

        updateList()

        val songToItem = fun(data: MusicData, song: Song): MediaItem {
            val albumPath = data.albums[song.album]?.path
            val uri = if (song.album == null)
                null
            else
                File(filesDir, "${url}icon/${albumPath}").toUri()
            return MediaItem.Builder()
                .setUri(File(filesDir, "${url}music/${song.path}").toUri())
                .setMediaMetadata(
                    MediaMetadata.Builder()
                        .setTitle(song.name)
                        .setAlbumTitle(song.album)
                        .setArtist(song.artist)
                        .setArtworkUri(uri)
                        .build()
                )
                .build()
        }

        executor.execute {

            // Callback when we click on a song
            list.onItemClickListener = AdapterView.OnItemClickListener { parent, v, position, id ->
                if (!shouldDisplaySongs()) {
                    // Clicked on a playlist element
                    currentPlaylist = data.playlists!!.keys.elementAt(position)
                    updateList()
                } else {
                    // Clicked on a song
                    val filteredData = downloaded.filter { currentPlaylist == null || it.playlist == currentPlaylist }
                    val song = filteredData[position]

                    //val controller = findViewById<PlayerView>(R.id.musicPlayer)
                    val selectedMusics = filteredData.filter { it.playlist == song.playlist && it.path != song.path }.shuffled().map { songToItem(data, it) }.toMutableList()
                    selectedMusics.add(0, songToItem(data, song))
                    controllerFuture.get().setMediaItems(selectedMusics)

                    controllerFuture.get().prepare()
                    controllerFuture.get().play()
                }
            }

            // Download missing songs
            data.musics.forEachIndexed{ index, song ->
                if (!File(filesDir, "${url}music/${song.path}").exists()) {
                    updateList()
                    builder.setContentText("$index / ${data.musics.size}")
                    notificationManager.notify(1, builder.build())
                    URL("https://${url}data/normalized/${song.path}").openStream().use { stream ->
                        FileOutputStream(File(filesDir, "${url}music/${song.path}")).use { output ->
                            stream.copyTo(output)
                        }
                    }
                    updateList()
                }
                val albumPath = data.albums[song.album]?.path
                if (song.album != null && !File(filesDir, "${url}icon/${albumPath}").exists()) {
                    updateList()
                    notificationManager.notify(1, builder.build())
                    URL("https://${url}data/icon/${albumPath}").openStream().use { stream ->
                        FileOutputStream(File(filesDir, "${url}icon/${albumPath}")).use { output ->
                            stream.copyTo(output)
                        }
                    }
                    updateList()
                }
                downloaded.add(0, song)
            }

            builder
                .setContentText("${data.musics.size} / ${data.musics.size}")
                .setOngoing(false)
            notificationManager.notify(1, builder.build())
            updateList()
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        //findViewById<PlayerView>(R.id.musicPlayer).player!!.release()
    }
}