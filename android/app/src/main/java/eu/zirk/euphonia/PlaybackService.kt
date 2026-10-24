package eu.zirk.euphonia

import android.os.Handler
import android.os.Looper
import androidx.annotation.OptIn
import androidx.media3.common.AudioAttributes
import androidx.media3.common.C
import androidx.media3.common.MediaItem
import androidx.media3.common.Player
import androidx.media3.common.util.UnstableApi
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.session.MediaSession
import androidx.media3.session.MediaSessionService
import androidx.media3.session.SessionResult
import androidx.preference.PreferenceManager
import com.google.common.util.concurrent.Futures
import com.google.common.util.concurrent.ListenableFuture

class PlaybackService : MediaSessionService(), MediaSession.Callback {
    lateinit var mediaSession: MediaSession
    var pauseFromNotification = false

    override fun onCreate() {
        super.onCreate()
        val player = ExoPlayer.Builder(this)
            .setAudioAttributes(
                AudioAttributes.Builder()
                    .setUsage(C.USAGE_MEDIA)
                    .setContentType(C.AUDIO_CONTENT_TYPE_MUSIC)
                    .build(),
                true
            )
            .setHandleAudioBecomingNoisy(true)
            .build()

        player.addListener(object : Player.Listener {
            override fun onPlayWhenReadyChanged(playWhenReady: Boolean, reason: Int) {
                val forcePlay = PreferenceManager.getDefaultSharedPreferences(applicationContext).getBoolean("force_unpause", false)
                if (forcePlay && !playWhenReady && pauseFromNotification)
                {
                    Handler(Looper.getMainLooper()).postDelayed({ player.play() }, 1000)
                }
                pauseFromNotification = false
            }
        })

        mediaSession = MediaSession.Builder(this, player).setCallback(this).build()
    }

    @OptIn(UnstableApi::class)
    override fun onPlayerCommandRequest(
        session: MediaSession,
        controller: MediaSession.ControllerInfo,
        playerCommand: Int
    ): Int {
        if (playerCommand == Player.COMMAND_PLAY_PAUSE) {
            pauseFromNotification = session.isMediaNotificationController(controller)
        }
        return SessionResult.RESULT_SUCCESS
    }

    override fun onGetSession(controllerInfo: MediaSession.ControllerInfo): MediaSession?
        = mediaSession

    override fun onDestroy() {
        mediaSession.run {
            player.release()
            release()
        }
        super.onDestroy()
    }

    override fun onAddMediaItems(
        mediaSession: MediaSession,
        controller: MediaSession.ControllerInfo,
        mediaItems: MutableList<MediaItem>
    ): ListenableFuture<MutableList<MediaItem>> {
        val updatedMediaItems = mediaItems.map { it.buildUpon().setUri(it.mediaId).build() }.toMutableList()

        return Futures.immediateFuture(updatedMediaItems)
    }
}
