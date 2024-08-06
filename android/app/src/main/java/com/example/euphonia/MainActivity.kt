package com.example.euphonia

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.ComponentName
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.ListView
import androidx.annotation.OptIn
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.widget.AppCompatImageButton
import androidx.core.app.NotificationCompat
import androidx.core.content.getSystemService
import androidx.media3.common.MediaItem
import androidx.media3.common.MediaMetadata
import androidx.media3.common.util.UnstableApi
import androidx.media3.session.MediaController
import androidx.media3.session.SessionToken
import androidx.media3.ui.PlayerControlView
import androidx.media3.ui.PlayerView
import com.example.euphonia.data.ExtendedSong
import com.example.euphonia.data.MusicData
import com.example.euphonia.data.Song
import com.google.common.util.concurrent.ListenableFuture
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

    var currUrl: String? = null
    lateinit var list: ListView

    lateinit var controllerFuture: ListenableFuture<MediaController>

    lateinit var data: MusicData

    override fun onBackPressed() {
        if (currentPlaylist != null) {
            currentPlaylist = null
            updateList()
        } else {
            onBackPressedDispatcher.onBackPressed()
        }
    }

    // We display the songs if there is no playlist available or if we chose one already
    fun shouldDisplaySongs(): Boolean {
        return data.playlists == null || data.playlists!!.isEmpty() || currentPlaylist != null;
    }

    fun updateList() {
        val handler = Handler(Looper.getMainLooper())
        handler.post {
            val adapter =
                if (shouldDisplaySongs()) {
                    SongAdapter(this, downloaded.filter { currentPlaylist == null || it.playlist == currentPlaylist }.map { ExtendedSong(it, data.albums[it.album]) }, currUrl!!)
                } else {
                    ArrayAdapter(this, android.R.layout.simple_list_item_1, data.playlists!!.map { it.value.name })
                }
            list.adapter = adapter
        }
    }

    fun onRandom(v: View) {
        val filteredData = downloaded.filter { currentPlaylist == null || it.playlist == currentPlaylist }
        val selectedMusics = filteredData.map { songToItem(data, it) }.shuffled().toMutableList()

        controllerFuture.get().setMediaItems(selectedMusics)

        controllerFuture.get().prepare()
        controllerFuture.get().play()
    }

    fun onRandomFromSong(position: Int) {

        val filteredData = downloaded.filter { currentPlaylist == null || it.playlist == currentPlaylist }
        val song = filteredData[position]

        val selectedMusics = filteredData.filter { it.playlist == song.playlist && it.path != song.path }.shuffled().map { songToItem(data, it) }.toMutableList()
        selectedMusics.add(0, songToItem(data, song))

        controllerFuture.get().setMediaItems(selectedMusics)

        controllerFuture.get().prepare()
        controllerFuture.get().play()
    }

    fun songToItem(data: MusicData, song: Song): MediaItem {
        val albumPath = data.albums[song.album]?.path
        val builder = MediaMetadata.Builder()
        if (song.album != null) {
            builder.setArtist(song.artist)
            builder.setArtworkUri(Uri.parse("${filesDir}/${currUrl}icon/${albumPath}"))
        } else {
            builder.setArtist(null)
            builder.setArtworkUri(Uri.parse("https://${currUrl}img/CD.png"))
        }
        return MediaItem.Builder()
            .setMediaId("${filesDir}/${currUrl}music/${song.path}")
            .setMediaMetadata(
                builder
                    .setTitle(song.name)
                    .setAlbumTitle(song.album)
                    .build()
            )
            .build()
    }

    @OptIn(UnstableApi::class)
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        // Ensure remote server is init
        val sharedPref = this.getSharedPreferences("settings", MODE_PRIVATE)
        currUrl = sharedPref.getString("remoteServer", null)
        if (currUrl == null) {
            val intent = Intent(applicationContext, SetupActivity::class.java)
            startActivity(intent)
        }

        //  Init media session
        val videoView = findViewById<PlayerView>(R.id.player)
        val sessionToken = SessionToken(this, ComponentName(this, PlaybackService::class.java))
        controllerFuture = MediaController.Builder(this, sessionToken).buildAsync()
        controllerFuture.addListener(
            {
                videoView.player = controllerFuture.get()
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
            .setSilent(true)
            .setOngoing(true)
        val channel = NotificationChannel("download_channel", "Data Download Progress", NotificationManager.IMPORTANCE_LOW)
        notificationManager.createNotificationChannel(channel)
        builder.setChannelId("download_channel")
        notificationManager.notify(1, builder.build())

        downloaded = mutableListOf()

        if (!File(filesDir, "${currUrl}info.json").exists()) {
            // Somehow the target file is missing? We need the user to go by the setup phase again
            val intent = Intent(applicationContext, SetupActivity::class.java)
            startActivity(intent)
            return
        }

        executor.execute {
            // Update data from remove server
            val text: String
            try {
                text = URL("https://${currUrl}?json=1").readText()
                File(filesDir, "${currUrl}info.json").writeText(text)
            } catch (e: Exception) {
                Log.e("Network Error", e.message.toString())
            }

            data = Gson().fromJson(File(filesDir, "${currUrl}info.json").readText(), MusicData::class.java)

            updateList()

            // Callback when we click on a song
            list.onItemClickListener = AdapterView.OnItemClickListener { parent, v, position, id ->
                if (!shouldDisplaySongs()) {
                    // Clicked on a playlist element
                    currentPlaylist = data.playlists!!.keys.elementAt(position)
                    updateList()
                } else {
                    // Clicked on a song
                    onRandomFromSong(position)
                }
            }

            // Download missing songs
            data.musics.forEachIndexed{ index, song ->
                if (!File(filesDir, "${currUrl}music/${song.path}").exists()) {
                    builder.setContentText("$index / ${data.musics.size}")
                    notificationManager.notify(1, builder.build())
                    try
                    {
                        URL("https://${currUrl}data/normalized/${song.path}").openStream().use { stream ->
                            FileOutputStream(File(filesDir, "${currUrl}music/${song.path}")).use { output ->
                                stream.copyTo(output)
                            }
                        }
                    }
                    catch (_: Exception)
                    { }
                }
                val albumPath = data.albums[song.album]?.path
                if (song.album != null && !File(filesDir, "${currUrl}icon/${albumPath}").exists()) {
                    notificationManager.notify(1, builder.build())
                    try
                    {
                        URL("https://${currUrl}data/icon/${albumPath}").openStream().use { stream ->
                            FileOutputStream(File(filesDir, "${currUrl}icon/${albumPath}")).use { output ->
                                stream.copyTo(output)
                            }
                        }
                    }
                    catch (_: Exception)
                    { }
                }
                downloaded.add(0, song)
                // TODO: Update list
            }

            builder
                .setContentText("${data.musics.size} / ${data.musics.size}")
                .setSilent(false)
                .setOngoing(false)
            notificationManager.notify(1, builder.build())
            updateList()
        }
    }

    override fun onDestroy() {
        super.onDestroy()
    }
}