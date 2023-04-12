package com.example.euphonia

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.support.v4.media.session.MediaSessionCompat
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.getSystemService
import com.google.android.exoplayer2.ExoPlayer
import com.google.android.exoplayer2.ext.mediasession.MediaSessionConnector
import com.google.android.exoplayer2.ui.PlayerNotificationManager
import com.google.android.exoplayer2.ui.PlayerView


class MainActivity : AppCompatActivity() {
    lateinit var mediaSession: MediaSessionCompat
    lateinit var mediaSessionConnector: MediaSessionConnector

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        val sharedPref = getPreferences(Context.MODE_PRIVATE)
        val remoteServer = sharedPref.getString("remoteServer", null)
        if (remoteServer == null) {
            val intent = Intent(applicationContext, SetupActivity::class.java)
            startActivity(intent)
        }

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
        findViewById<PlayerView>(R.id.musicPlayer).player!!.release()
        mediaSession.release()
    }
}