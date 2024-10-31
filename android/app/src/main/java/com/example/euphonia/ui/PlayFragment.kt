package com.example.euphonia.ui

import android.graphics.BitmapFactory
import android.os.Bundle
import androidx.fragment.app.Fragment
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.media3.common.MediaItem
import androidx.media3.common.MediaMetadata
import androidx.media3.common.Player
import androidx.media3.common.util.UnstableApi
import androidx.media3.ui.PlayerView
import com.example.euphonia.R
import com.example.euphonia.MainActivity
import com.google.common.util.concurrent.MoreExecutors

class PlayFragment : Fragment() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
    }

    fun showAlbum(view: View, metadata: MediaMetadata?) {
        val player =  view.findViewById<ImageView>(R.id.playerImage)
        val desc = view.findViewById<TextView>(R.id.playerDescription)
        if (player != null) {
            val bmp = BitmapFactory.decodeFile(metadata?.artworkUri?.path)
            if (bmp != null) {
                player.setImageBitmap(bmp)
            }

            if (metadata?.title != null) {
                desc.text = "${metadata?.title} by ${metadata?.artist}"
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

        pView.controllerFuture?.addListener(
            {
                val player = pView.controllerFuture!!.get()
                videoView.player = player
                player.addListener(object : Player.Listener {
                    override fun onMediaItemTransition(mediaItem: MediaItem?, reason: Int) {
                        showAlbum(view, mediaItem?.mediaMetadata)
                    }
                })
                showAlbum(view, pView.controllerFuture!!.get().mediaMetadata)
            },
            MoreExecutors.directExecutor()
        )

        videoView.showController()

        return view
    }
}