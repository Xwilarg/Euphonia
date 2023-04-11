package com.example.euphonia

import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.storage.StorageManager
import android.util.Log
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.EditText
import android.widget.ListView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.NotificationCompat
import androidx.core.content.getSystemService
import com.example.euphonia.data.MusicData
import com.google.gson.Gson
import java.io.File
import java.io.FileOutputStream
import java.net.URL
import java.util.*
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors


class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)
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

        list.onItemClickListener = AdapterView.OnItemClickListener { parent, v, position, id ->
            Log.i("TEST", position.toString())
        }

        val builder = NotificationCompat.Builder(this, "download_channel")
            .setContentTitle("Updating data...")
            .setSmallIcon(R.drawable.icon)
            .setPriority(NotificationCompat.PRIORITY_DEFAULT)
            .setOngoing(true)
        val channel = NotificationChannel("download_channel", "Download Channel", NotificationManager.IMPORTANCE_DEFAULT)
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

        executor.execute {
            val data = Gson().fromJson(URL("https://${url}php/getInfoJson.php").readText(), MusicData::class.java)

            val files = this.fileList()

            data.musics.forEachIndexed{ index, song ->
                if (!files.contains(song.path)) {
                    updateList()
                    builder.setContentText("$index / ${data.musics.size}")
                    notificationManager.notify(1, builder.build())
                    URL("https://${url}data/normalized/${song.path}").openStream().use { stream ->
                        FileOutputStream(File(filesDir, song.path)).use { output ->
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