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
import android.widget.Toast
import androidx.core.content.getSystemService
import androidx.lifecycle.lifecycleScope
import com.example.euphonia.data.MusicData
import com.example.euphonia.data.Metadata
import com.example.euphonia.data.Song
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import okhttp3.Call
import okhttp3.Callback
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody
import okhttp3.Response
import java.io.File
import java.io.FileOutputStream
import java.io.IOException
import java.net.URL

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    var currUrl: String? = null

    // Current playlist that need to be displayed
    var currentPlaylist: String? = null

    var controllerFuture: ListenableFuture<MediaController>? = null
    // List of songs that were downloaded
    var downloaded: MutableList<Song> = mutableListOf()

    var data: MusicData = MusicData(arrayOf(), arrayOf(), null, mapOf(), null)
    lateinit var metadata: Metadata

    fun init() {
        // Ensure remote server is init
        val sharedPref = this.getSharedPreferences("settings", MODE_PRIVATE)
        val index = sharedPref.getInt("currentServer", -1)
        if (index == -1) {
            val intent = Intent(applicationContext, SetupActivity::class.java)
            startActivity(intent)
            return
        }

        //  Init media session
        val sessionToken = SessionToken(this, ComponentName(this, PlaybackService::class.java))
        controllerFuture = MediaController.Builder(this, sessionToken).buildAsync()

        loadData()
    }

    fun loadData() {
        val sharedPref = this.getSharedPreferences("settings", MODE_PRIVATE)
        val index = sharedPref.getInt("currentServer", -1)

        val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
        currUrl = servers.elementAt(index)

        // Update JSON info
        val executor: ExecutorService = Executors.newSingleThreadExecutor()

        val notificationManager = this.getSystemService<NotificationManager>()!!

        val builder = NotificationCompat.Builder(this, "download_channel")
            .setContentTitle(applicationContext.getString(R.string.main_data_update))
            .setSmallIcon(R.mipmap.ic_launcher)
            .setPriority(NotificationCompat.PRIORITY_DEFAULT)
            .setSilent(true)
            .setOngoing(true)
        val channel = NotificationChannel("download_channel", "Data Download Progress", NotificationManager.IMPORTANCE_LOW)
        notificationManager.createNotificationChannel(channel)
        builder.setChannelId("download_channel")
        notificationManager.notify(1, builder.build())

        downloaded = mutableListOf()

        data = MusicData(arrayOf<Song>(), arrayOf<String>(), emptyMap(), emptyMap(), emptyMap())

        if (!File(filesDir, "${currUrl}info.json").exists()) {
            // Somehow the target file is missing? We need the user to go by the setup phase again
            val intent = Intent(applicationContext, SetupActivity::class.java)
            startActivity(intent)
            return
        }

        try {
            data = Gson().fromJson(File(filesDir, "${currUrl}info.json").readText(), MusicData::class.java)
            data.musics = data.musics.filter { !it.isArchived }.toTypedArray()
            downloaded = data.musics.toMutableList()
            metadata = Gson().fromJson(File(filesDir, "${currUrl}metadata.json").readText(), Metadata::class.java)
        }
        catch (e: Exception) { }

        var adminToken = sharedPref.getString("adminToken", null)
        if (adminToken != null) {
            executor.execute {
                val okHttpClient = OkHttpClient()

                val requestBody = RequestBody.create(null, ByteArray(0))
                val request = Request.Builder()
                    .post(requestBody)
                    .addHeader("Authorization", "Bearer ${adminToken}")
                    .url("https://${servers.elementAt(index)}api/auth/validate")
                    .build()
                okHttpClient.newCall(request).enqueue(object : Callback {
                    override fun onFailure(call: Call, e: IOException) { }

                    override fun onResponse(call: Call, response: Response) {
                        if (response.code != 200) {
                            with(sharedPref.edit()) {
                                putString("adminToken", null)
                                apply()
                            }
                            lifecycleScope.launch(Dispatchers.Main) {
                                Toast.makeText(applicationContext, "Admin token had expired", Toast.LENGTH_SHORT).show()
                            }
                        }
                    }
                })
            }
        }

        executor.execute {
            var songDownloaded = 0
            var newDownloads = mutableListOf<Song>()
            // Update data from remove server
            val text: String
            try {
                text = URL("https://${currUrl}?json=1").readText()
                File(filesDir, "${currUrl}info.json").writeText(text)
                data = Gson().fromJson(File(filesDir, "${currUrl}info.json").readText(), MusicData::class.java)
                if (data.musics === null) { // No music available, nothing to do for now!
                    lifecycleScope.launch(Dispatchers.Main) {
                        Toast.makeText(
                            applicationContext,
                            applicationContext.getString(R.string.no_song_available),
                            Toast.LENGTH_LONG
                        ).show()
                        data.musics = arrayOf()
                    }
                    return@execute
                }
                data.musics = data.musics.filter { !it.isArchived }.toTypedArray()
            } catch (e: Exception) {
                lifecycleScope.launch(Dispatchers.Main) {
                    Toast.makeText(applicationContext, "Failed to update info JSON", Toast.LENGTH_SHORT).show()
                }
                return@execute
            }

            try {
                val metadataText = URL("https://${currUrl}/data/metadata.json").readText()
                File(filesDir, "${currUrl}metadata.json").writeText(metadataText)
                metadata = Gson().fromJson(File(filesDir, "${currUrl}metadata.json").readText(), Metadata::class.java)
            } catch (e: Exception) {
                lifecycleScope.launch(Dispatchers.Main) {
                    Toast.makeText(applicationContext, "Failed to update metadata JSON", Toast.LENGTH_SHORT).show()
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
                        songDownloaded++
                    }
                    catch (_: Exception)
                    { }
                }
                if (data.albumHashes != null && song.thumbnailHash != null) {
                    val hashPath = data.albumHashes!![song.thumbnailHash]
                    if (!File(filesDir, "${currUrl}icon/${hashPath}").exists()) {
                        notificationManager.notify(1, builder.build())
                        try
                        {
                            URL("https://${currUrl}data/icon/${hashPath}").openStream().use { stream ->
                                FileOutputStream(File(filesDir, "${currUrl}icon/${hashPath}")).use { output ->
                                    stream.copyTo(output)
                                }
                            }
                        }
                        catch (_: Exception)
                        { }
                    }
                }
                else if (song.album != null) {
                    val albumPath = data.albums[song.album]?.path
                    if (!File(filesDir, "${currUrl}icon/${albumPath}").exists()) {
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
                }
                newDownloads.add(0, song)
                // TODO: Update list
            }

            downloaded.reverse()
            newDownloads = downloaded
            builder
                .setContentText(if (songDownloaded == 0) resources.getString(R.string.music_updated) else resources.getString(R.string.music_updated_with_value, songDownloaded))
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