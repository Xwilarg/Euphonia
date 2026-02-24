package eu.zirk.euphonia

import android.content.Intent
import android.content.pm.PackageManager
import android.os.Bundle
import android.view.View
import android.widget.*
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import java.io.File
import java.net.URL
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors

class SetupActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_setup)

        val sharedPref = this.getSharedPreferences("permissions", MODE_PRIVATE)
        if (checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_DENIED
            && (shouldShowRequestPermissionRationale(android.Manifest.permission.POST_NOTIFICATIONS) || sharedPref.getBoolean("post_first", true)))
        {
            ActivityCompat.requestPermissions(this, arrayOf(android.Manifest.permission.POST_NOTIFICATIONS), 1)
            with (sharedPref.edit())
            {
                putBoolean("post_first", false)
                apply()
            }
        }
    }

    override fun onBackPressed() {
        val sharedPref = this.getSharedPreferences("settings", MODE_PRIVATE)
        if (sharedPref.getInt("currentServer", -1) != -1) {
            super.onBackPressed()
        }
    }

    fun updateData(view: View) {
        val input = findViewById<EditText>(R.id.inputURL)
        var url = input.text.toString()

        if (!url.endsWith("/")) {
            url += "/";
        }
        // Download JSON data
        val executor: ExecutorService = Executors.newSingleThreadExecutor()
        executor.execute {
            val text: String
            try {
                text = URL("https://${url}api/data/info").readText()
            } catch (e: Exception) {
                findViewById<TextView>(R.id.error).text = e.message.toString()
                input.text.clear()
                return@execute
            }

            val mDir = File(filesDir, "${url}music")
            if (!mDir.exists()) mDir.mkdirs()
            val lDir = File(filesDir, "${url}icon")
            if (!lDir.exists()) lDir.mkdirs()

            File(filesDir, "${url}info.json").writeText(text)

            val sharedPref = this.getSharedPreferences("settings", MODE_PRIVATE)
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