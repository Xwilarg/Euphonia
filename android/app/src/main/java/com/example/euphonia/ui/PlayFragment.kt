package com.example.euphonia.ui

import android.content.Context.MODE_PRIVATE
import android.content.Intent
import android.graphics.BitmapFactory
import android.os.Bundle
import androidx.fragment.app.Fragment
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageButton
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.annotation.OptIn
import androidx.lifecycle.lifecycleScope
import androidx.media3.common.MediaItem
import androidx.media3.common.MediaMetadata
import androidx.media3.common.Player
import androidx.media3.common.util.UnstableApi
import androidx.media3.session.MediaController
import androidx.media3.ui.PlayerView
import com.example.euphonia.R
import com.example.euphonia.MainActivity
import com.google.common.util.concurrent.MoreExecutors
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import okhttp3.Call
import okhttp3.Callback
import okhttp3.FormBody
import okhttp3.OkHttpClient
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.Request
import okhttp3.RequestBody
import okhttp3.Response
import java.io.IOException
import java.net.URLEncoder

class PlayFragment : Fragment() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
    }

    @OptIn(UnstableApi::class)
    fun showMedia(view: View, metadata: MediaMetadata?, med: MediaController?) {
        val player =  view.findViewById<ImageView>(R.id.playerImage)
        val desc = view.findViewById<TextView>(R.id.playerDescription)
        if (player != null) {
            view.findViewById<ImageButton>(R.id.delete).setOnClickListener {
                val key = metadata?.displayTitle // Display title is not used so store the song key
                if (key != null) {
                    val okHttpClient = OkHttpClient()

                    val sharedPref = requireContext().getSharedPreferences("settings", MODE_PRIVATE)
                    val adminToken = sharedPref.getString("adminToken", null)
                    val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
                    val index = sharedPref.getInt("currentServer", -1)

                    val requestBody = FormBody.Builder()
                    requestBody.add("Key", key.toString())
                    val request = Request.Builder()
                        .post(requestBody.build())
                        .addHeader("Authorization", "Bearer ${adminToken}")
                        .url("https://${servers.elementAt(index)}api/data/archive")
                        .build()
                    okHttpClient.newCall(request).enqueue(object : Callback {
                        override fun onFailure(call: Call, e: IOException) { }

                        override fun onResponse(call: Call, response: Response) {
                            if (response.code != 200) {
                                lifecycleScope.launch(Dispatchers.Main) {
                                    Toast.makeText(requireContext(), "Failed to archive song: ${response.code}", Toast.LENGTH_SHORT).show()
                                }
                            } else {
                                lifecycleScope.launch(Dispatchers.Main) {
                                    med?.next()
                                }
                                // TODO: Update list and stuff
                            }
                        }
                    })
                }
            }

            view.findViewById<ImageButton>(R.id.share).setOnClickListener {
                val key = metadata?.displayTitle
                if (key != null) {
                    val sharedPref = requireContext().getSharedPreferences("settings", MODE_PRIVATE)
                    val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
                    val index = sharedPref.getInt("currentServer", -1)

                    val intent= Intent()
                    intent.action = Intent.ACTION_SEND
                    intent.putExtra(Intent.EXTRA_TEXT, "https://" + servers.elementAt(index) + "?song=" + URLEncoder.encode(key.toString(), "utf-8"))
                    intent.type = "text/plain"
                    startActivity(Intent.createChooser(intent,"Share To:"))
                }
            }

            player.setImageBitmap(null)
            val bmp = BitmapFactory.decodeFile(metadata?.artworkUri?.path)
            if (bmp != null) {
                player.setImageBitmap(bmp)
            }

            if (metadata?.title != null) {
                desc.text = "${metadata?.title} ${resources.getString(R.string.by)} ${metadata?.artist}"
            } else {
                desc.text = ""
            }
        }
    }

    @UnstableApi
    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        val view = inflater.inflate(R.layout.fragment_play, container, false)
        val videoView = view.findViewById<PlayerView>(R.id.player)

        val pView = requireActivity() as MainActivity

        val sharedPref = requireContext().getSharedPreferences("settings", MODE_PRIVATE)
        val adminToken = sharedPref.getString("adminToken", null)

        view.findViewById<ImageButton>(R.id.delete).visibility = if (adminToken == null) {
            View.GONE
        } else {
            View.VISIBLE
        }

        pView.controllerFuture?.addListener(
            {
                val player = pView.controllerFuture!!.get()
                videoView.player = player
                player.addListener(object : Player.Listener {
                    override fun onMediaItemTransition(mediaItem: MediaItem?, reason: Int) {
                        showMedia(view, mediaItem?.mediaMetadata, player)
                    }
                })
                showMedia(view, player.mediaMetadata, player)
            },
            MoreExecutors.directExecutor()
        )

        videoView.showController()

        return view
    }
}