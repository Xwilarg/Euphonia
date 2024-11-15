package com.example.euphonia.ui

import android.app.AlertDialog
import android.content.Context.MODE_PRIVATE
import android.content.DialogInterface
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import androidx.preference.Preference
import androidx.preference.PreferenceFragmentCompat
import com.example.euphonia.MainActivity
import com.example.euphonia.R
import com.example.euphonia.SetupActivity
import java.io.File

class SettingsFragment : PreferenceFragmentCompat() {

    var deleteConfirm = false

    override fun onCreatePreferences(savedInstanceState: Bundle?, rootKey: String?) {
        setPreferencesFromResource(R.xml.root_preferences, rootKey)

        findPreference<Preference>("website")!!.setOnPreferenceClickListener {
            val sharedPref = requireContext().getSharedPreferences("settings", MODE_PRIVATE)
            val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
            val index = sharedPref.getInt("currentServer", -1)
            startActivity(
                Intent(
                    Intent.ACTION_VIEW,
                    Uri.parse("https://${servers.elementAt(index)}")
                )
            )
            true
        }

        findPreference<Preference>("github")!!.setOnPreferenceClickListener {
            startActivity(
                Intent(
                    Intent.ACTION_VIEW,
                    Uri.parse("https://github.com/Xwilarg/Euphonia")
                )
            )
            true
        }

        findPreference<Preference>("source_add")!!.setOnPreferenceClickListener {
            val builder = AlertDialog.Builder(activity)
            builder.setTitle(R.string.pref_source_set)

            val sharedPref = requireContext().getSharedPreferences("settings", MODE_PRIVATE)
            val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!

            val options = servers.toMutableList()
            options.add(resources.getString(R.string.pref_source_add))

            val cleanOptions = options.toTypedArray()

            builder.setItems(cleanOptions) { _: DialogInterface, which: Int ->
                if (which == servers.count()) {
                    val intent = Intent(requireContext(), SetupActivity::class.java)
                    startActivity(intent)
                } else {
                    with(sharedPref.edit()) {
                        putInt("currentServer", which)
                        apply()
                    }
                    (requireActivity() as MainActivity).loadData()
                }
            }
            builder.show()

            true
        }

        findPreference<Preference>("source_remove")!!.setOnPreferenceClickListener {
            if (deleteConfirm) {
                val sharedPref = requireContext().getSharedPreferences("settings", MODE_PRIVATE)
                val index = sharedPref.getInt("currentServer", -1)

                val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
                val currUrl = servers.elementAt(index)
                File(requireContext().filesDir, currUrl).deleteRecursively()

                val intent = Intent(requireContext(), MainActivity::class.java)
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK)
                startActivity(intent)
                Runtime.getRuntime().exit(0)

            } else {
                findPreference<Preference>("source_remove")!!.title =
                    resources.getString(R.string.pref_deletion_confirm)

                deleteConfirm = true
            }
            true
        }
    }
}