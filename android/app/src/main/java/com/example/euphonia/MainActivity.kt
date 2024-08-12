package com.example.euphonia

import android.app.NotificationManager
import android.content.ComponentName
import android.os.Bundle
import com.google.android.material.bottomnavigation.BottomNavigationView
import androidx.appcompat.app.AppCompatActivity
import androidx.navigation.findNavController
import androidx.navigation.ui.AppBarConfiguration
import androidx.navigation.ui.setupActionBarWithNavController
import androidx.navigation.ui.setupWithNavController
import com.example.euphonia.databinding.ActivityMainBinding
import android.content.Intent
import androidx.media3.session.MediaController
import androidx.media3.session.SessionToken
import com.google.common.util.concurrent.ListenableFuture
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors
import androidx.core.app.NotificationCompat
import android.app.NotificationChannel
import android.util.Log
import androidx.core.content.getSystemService
import com.example.euphonia.data.MusicData
import com.example.euphonia.data.Song
import com.example.euphonia.ui.MusicFragment
import com.google.gson.Gson
import java.io.File
import java.io.FileOutputStream
import java.net.URL

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    var currUrl: String? = null

    // Current playlist that need to be displayed
    var currentPlaylist: String? = null

    lateinit var controllerFuture: ListenableFuture<MediaController>
    // List of songs that were downloaded
    var downloaded: MutableList<Song> = mutableListOf()

    lateinit var data: MusicData

    fun init() {
        // Ensure remote server is init
        val sharedPref = this.getSharedPreferences("settings", MODE_PRIVATE)
        currUrl = sharedPref.getString("remoteServer", null)
        if (currUrl == null) {
            val intent = Intent(applicationContext, SetupActivity::class.java)
            startActivity(intent)
        }

        //  Init media session
        val sessionToken = SessionToken(this, ComponentName(this, PlaybackService::class.java))
        controllerFuture = MediaController.Builder(this, sessionToken).buildAsync()

        // Update JSON info
        val executor: ExecutorService = Executors.newSingleThreadExecutor()

        val notificationManager = this.getSystemService<NotificationManager>()!!

        val builder = NotificationCompat.Builder(this, "download_channel")
            .setContentTitle(applicationContext.getString(R.string.main_dataUpdate))
            .setSmallIcon(R.drawable.icon)
            .setPriority(NotificationCompat.PRIORITY_DEFAULT)
            .setSilent(true)
            .setOngoing(true)
        val channel = NotificationChannel("download_channel", "Data Download Progress", NotificationManager.IMPORTANCE_LOW)
        notificationManager.createNotificationChannel(channel)
        builder.setChannelId("download_channel")
        notificationManager.notify(1, builder.build())

        downloaded = mutableListOf()

        data = MusicData(arrayOf<Song>(), arrayOf<String>(), emptyMap(), emptyMap())

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
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        init()

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        val navView: BottomNavigationView = binding.navView

        val navController = findNavController(R.id.nav_host_fragment_activity_main)
        // Passing each menu ID as a set of Ids because each
        // menu should be considered as top level destinations.
        val appBarConfiguration = AppBarConfiguration(
            setOf(
                R.id.navigation_play, R.id.navigation_music, R.id.navigation_settings
            )
        )
        setupActionBarWithNavController(navController, appBarConfiguration)
        navView.setupWithNavController(navController)
    }
}