package com.example.euphonia.ui

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import androidx.preference.Preference
import androidx.preference.PreferenceFragmentCompat
import com.example.euphonia.R

class SettingsFragment : PreferenceFragmentCompat() {

    override fun onCreatePreferences(savedInstanceState: Bundle?, rootKey: String?) {
        setPreferencesFromResource(R.xml.root_preferences, rootKey)

        findPreference<Preference>("website")!!.setOnPreferenceClickListener {
            val sharedPref = requireContext().getSharedPreferences("settings", Context.MODE_PRIVATE)
            startActivity(Intent(Intent.ACTION_VIEW, Uri.parse("https://${sharedPref.getString("remoteServer", null)}")))
            true
        }
    }
}