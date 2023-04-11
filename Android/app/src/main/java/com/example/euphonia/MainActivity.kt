package com.example.euphonia

import android.content.Context
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.storage.StorageManager
import android.util.Log
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.EditText
import android.widget.ListView
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.getSystemService
import com.example.euphonia.data.MusicData
import com.google.gson.Gson
import java.net.URL
import java.nio.file.Files
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

        var dial: AlertDialog? = null
        val storageManager = this.getSystemService<StorageManager>()!!
        val uuid: UUID = storageManager.getUuidForPath(filesDir)
        
        list.onItemClickListener = AdapterView.OnItemClickListener { parent, v, position, id ->
            Log.i("TEST", position.toString())
        }

        executor.execute {
            val data = Gson().fromJson(URL("https://${url}php/getInfoJson.php").readText(), MusicData::class.java)
            val musics = data.musics.map { it.name }
            val adapter = ArrayAdapter<String>(this, android.R.layout.simple_list_item_1, musics)

            val files = this.fileList()

            handler.post {
                val downloadBar = AlertDialog.Builder(this)
                downloadBar.setMessage("Updating data...")
                dial = downloadBar.create()
                dial!!.setCanceledOnTouchOutside(false)
                dial!!.show()
            }

            data.musics.forEachIndexed{ index, song ->
                if (!files.contains(song.path)) {
                    this.openFileOutput(song.path, Context.MODE_PRIVATE).use {
                        dial!!.setMessage("Updating data... $index / ${musics.size}")
                        URL("https://${url}data/normalized/${song.path}").openStream().use { stream ->
                            val bytes = stream.readBytes()
                            storageManager.allocateBytes(uuid, bytes.size.toLong())
                            it.write(bytes)
                        }
                    }
                }
            }

            handler.post {
                dial!!.dismiss()
                list.adapter = adapter
            }
        }
    }
}