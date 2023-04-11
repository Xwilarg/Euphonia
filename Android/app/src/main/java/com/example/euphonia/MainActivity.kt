package com.example.euphonia

import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.view.View
import android.widget.EditText
import androidx.appcompat.app.AppCompatActivity
import com.example.euphonia.data.MusicData
import com.google.gson.Gson
import java.net.URL
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
        url = "https://${url}php/getInfoJson.php"
        val executor: ExecutorService = Executors.newSingleThreadExecutor()
        val handler = Handler(Looper.getMainLooper())

        executor.execute {
            val data = Gson().fromJson(URL(url).readText(), MusicData::class.java)
            Log.i("JSON", "")
            handler.post {}
        }
    }
}