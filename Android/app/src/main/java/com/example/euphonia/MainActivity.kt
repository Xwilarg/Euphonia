package com.example.euphonia

import androidx.appcompat.app.AppCompatActivity
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.EditText
import java.net.URL

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
        url += "php/getInfoJson.php"
        Log.i("JSON", URL(url).readText())
    }
}