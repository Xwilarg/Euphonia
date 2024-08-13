package com.example.euphonia

import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.*
import androidx.appcompat.app.AppCompatActivity
import java.io.File
import java.net.URL
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors

class SetupActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_setup)
    }

    fun updateData(view: View) {
        var url = findViewById<EditText>(R.id.inputURL).text.toString()

        if (!url.endsWith("/")) {
            url += "/";
        }

        val executor: ExecutorService = Executors.newSingleThreadExecutor()
        executor.execute {
            // Download JSON data
            val text: String
            try {
                text = URL("https://${url}php/getInfoJson.php").readText()
            } catch (e: Exception) {
                Log.e("Network Error", e.message.toString())
                return@execute
            }

            val mDir = File(filesDir, "${url}music")
            if (!mDir.exists()) mDir.mkdirs()
            val lDir = File(filesDir, "${url}icon")
            if (!lDir.exists()) lDir.mkdirs()

            File(filesDir, "${url}info.json").writeText(text)

            val sharedPref = this.getSharedPreferences("settings", Context.MODE_PRIVATE)
            with(sharedPref.edit()) {
                val data = sharedPref.getStringSet("remoteServers", emptySet<String>())!!
                val editableData = data.toMutableList()
                if (!data.contains(url)) {
                    editableData.add(url)
                    putStringSet("remoteServers", editableData.toSet())
                }
                putInt("currentServer", 0)
                apply()
            }

            val intent = Intent(applicationContext, MainActivity::class.java)
            startActivity(intent)
        }
    }
}