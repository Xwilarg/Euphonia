package com.example.euphonia.ui

import android.os.Bundle
import androidx.fragment.app.Fragment
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.media3.ui.PlayerView
import com.example.euphonia.R
import com.example.euphonia.MainActivity
import com.google.common.util.concurrent.MoreExecutors

class PlayFragment : Fragment() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
    }

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        val view = inflater.inflate(R.layout.fragment_play, container, false)
        val videoView = view.findViewById<PlayerView>(R.id.player)

        val pView = requireActivity() as MainActivity

        pView.controllerFuture.addListener(
            {
                videoView.player = pView.controllerFuture.get()
            },
            MoreExecutors.directExecutor()
        )

        return view
    }
}