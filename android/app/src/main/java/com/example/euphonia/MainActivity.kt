package com.example.euphonia

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.support.v4.media.session.MediaSessionCompat
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.ListView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.NotificationCompat
import androidx.core.content.getSystemService
import androidx.core.net.toUri
import com.example.euphonia.data.MusicData
import com.example.euphonia.data.Song
import com.google.android.exoplayer2.ExoPlayer
import com.google.android.exoplayer2.MediaItem
import com.google.android.exoplayer2.MediaMetadata
import com.google.android.exoplayer2.ext.mediasession.MediaSessionConnector
import com.google.android.exoplayer2.ui.PlayerNotificationManager
import com.google.android.exoplayer2.ui.PlayerView
import com.google.gson.Gson
import java.io.File
import java.io.FileOutputStream
import java.net.URL
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors


class MainActivity : AppCompatActivity() {
    lateinit var mediaSession: MediaSessionCompat
    lateinit var mediaSessionConnector: MediaSessionConnector

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        // Ensure remote server is init
        val sharedPref = this.getSharedPreferences("settings", Context.MODE_PRIVATE)
        val url = sharedPref.getString("remoteServer", null)
        if (url == null) {
            val intent = Intent(applicationContext, SetupActivity::class.java)
            startActivity(intent)
        }

        //  Init media session
        val musicPlayer = findViewById<PlayerView>(R.id.musicPlayer)
        musicPlayer.player = ExoPlayer.Builder(this).build()
        mediaSession = MediaSessionCompat(this, "music")
        mediaSessionConnector = MediaSessionConnector(mediaSession)
        mediaSessionConnector.setPlayer(musicPlayer.player)
        val currSongNotifChannel = NotificationChannel("music_channel", "Current Song", NotificationManager.IMPORTANCE_HIGH)
        this.getSystemService<NotificationManager>()!!.createNotificationChannel(currSongNotifChannel)
        val pNotifManager = PlayerNotificationManager.Builder(this, 2, "music_channel")
            .setSmallIconResourceId(R.drawable.icon)
            .build()
        pNotifManager.setMediaSessionToken(mediaSession.sessionToken)
        pNotifManager.setPlayer(musicPlayer.player)

        // Update JSON info
        val executor: ExecutorService = Executors.newSingleThreadExecutor()
        val handler = Handler(Looper.getMainLooper())

        val list = findViewById<ListView>(R.id.musicData)

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

        val downloaded = mutableListOf<String>()

        val updateList = {
            handler.post {
                val adapter = ArrayAdapter(this, android.R.layout.simple_list_item_1, downloaded)
                list.adapter = adapter
            }
        }

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

        val data = Gson().fromJson(File(filesDir, "${url}info.json").readText(), MusicData::class.java)

        executor.execute {

            // Callback when we click on a song
            list.onItemClickListener = AdapterView.OnItemClickListener { parent, v, position, id ->
                val song = data.musics[data.musics.size - position - 1]

                val controller = findViewById<PlayerView>(R.id.musicPlayer)
                val selectedMusics = data.musics.filter { it.playlist == song.playlist && it.path != song.path }.shuffled().map { songToItem(data, it) }.toMutableList()
                selectedMusics.add(0, songToItem(data, song))
                controller.player!!.setMediaItems(selectedMusics)

                controller.player!!.prepare()
                controller.player!!.play()
            }

            // Download missing songs
            data.musics.reversed().forEachIndexed{ index, song ->
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
                downloaded.add(song.name)
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
        findViewById<PlayerView>(R.id.musicPlayer).player!!.release()
        mediaSession.release()
    }
}