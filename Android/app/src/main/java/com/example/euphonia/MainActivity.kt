package com.example.euphonia

import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.support.v4.media.session.MediaSessionCompat
import android.view.View
import android.widget.*
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
import com.google.android.exoplayer2.ui.StyledPlayerView
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
        val musicPlayer = findViewById<PlayerView>(R.id.musicPlayer)
        musicPlayer.player = ExoPlayer.Builder(this).build()
        mediaSession = MediaSessionCompat(this, "music")
        mediaSessionConnector = MediaSessionConnector(mediaSession)
        mediaSessionConnector.setPlayer(musicPlayer.player)
        val channel = NotificationChannel("music_channel", "Current Song", NotificationManager.IMPORTANCE_HIGH)
        this.getSystemService<NotificationManager>()!!.createNotificationChannel(channel)
        val notificationManager = PlayerNotificationManager.Builder(this, 2, "music_channel")
            .setSmallIconResourceId(R.drawable.icon)
            .build()
        notificationManager.setMediaSessionToken(mediaSession.sessionToken)
        notificationManager.setPlayer(musicPlayer.player)
    }

    override fun onDestroy() {
        super.onDestroy()
        findViewById<StyledPlayerView>(R.id.musicPlayer).player!!.release()
        mediaSession.release()
    }

    fun updateData(view: View) {
        var url = findViewById<EditText>(R.id.inputURL).text.toString()

        if (!url.endsWith("/")) {
            url += "/";
        }
        val executor: ExecutorService = Executors.newSingleThreadExecutor()
        val handler = Handler(Looper.getMainLooper())

        val list = findViewById<ListView>(R.id.musicData)

        val notificationManager = this.getSystemService<NotificationManager>()!!

        val updateButton = findViewById<Button>(R.id.refreshData)
        updateButton.isClickable = false
        updateButton.alpha = .5f

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

        val mDir = File(filesDir, "${url}music")
        if (!mDir.exists()) mDir.mkdirs()
        val lDir = File(filesDir, "${url}icon")
        if (!lDir.exists()) lDir.mkdirs()

        val songToItem = fun(data: MusicData, song: Song):MediaItem {
            val albumPath = data.albums[song.album]?.path
            return MediaItem.Builder()
            .setUri(File(filesDir, "${url}music/${song.path}").toUri())
            .setMediaMetadata(
                MediaMetadata.Builder()
                    .setTitle(song.name)
                    .setAlbumTitle(song.album)
                    .setArtist(song.artist)
                    .setArtworkUri(if (albumPath == null) null else File(filesDir, "${url}icon/${albumPath}").toUri())
                    .build()
            )
            .build()
        }

        executor.execute {
            // Download JSON data
            val data = Gson().fromJson(URL("https://${url}php/getInfoJson.php").readText(), MusicData::class.java)

            // Callback when we click on a song
            list.onItemClickListener = AdapterView.OnItemClickListener { parent, v, position, id ->
                val song = data.musics[position]

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
                if (albumPath != null && !File(filesDir, "${url}icon/${albumPath}").exists()) {
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

            handler.post {
                updateButton.isClickable = true
                updateButton.alpha = 1f
            }
        }
    }
}