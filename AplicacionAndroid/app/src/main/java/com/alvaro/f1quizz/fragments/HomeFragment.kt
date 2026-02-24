package com.alvaro.f1quizz.fragments

import android.content.Context
import android.content.SharedPreferences
import android.media.MediaPlayer
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.navigation.NavController
import androidx.navigation.Navigation
import com.alvaro.f1quizz.R
import com.alvaro.f1quizz.databinding.FragmentHomeBinding
import com.google.firebase.auth.FirebaseAuth

class HomeFragment : Fragment() {
    private lateinit var navController: NavController
    private lateinit var binding: FragmentHomeBinding
    private lateinit var auth: FirebaseAuth
    private var mediaPlayer: MediaPlayer? = null

    private lateinit var sharedPreferences: SharedPreferences
    private var isMuted = false

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        binding = FragmentHomeBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        navController = Navigation.findNavController(view)
        auth = FirebaseAuth.getInstance()

        sharedPreferences = requireActivity().getSharedPreferences("AppPreferences", Context.MODE_PRIVATE)
        isMuted = sharedPreferences.getBoolean("isMuted", false)

        setupMusic()

        binding.cvMute.setOnClickListener {
            toggleMute()
        }

        binding.btnStartQuiz.setOnClickListener {
            navController.navigate(R.id.action_homeFragment_to_quizFragment)
        }

        binding.btnShowStats.setOnClickListener {
            navController.navigate(R.id.action_homeFragment_to_statsFragment)
        }

        binding.logoutBtn.setOnClickListener {
            auth.signOut()
            val navOptions = androidx.navigation.NavOptions.Builder()
                .setPopUpTo(R.id.homeFragment, true)
                .build()
            navController.navigate(R.id.action_homeFragment_to_logInFragment, null, navOptions)
            Toast.makeText(context, getString(R.string.logout), Toast.LENGTH_SHORT).show()
        }
    }

    private fun setupMusic() {
        if (mediaPlayer == null) {
            mediaPlayer = MediaPlayer.create(requireContext(), R.raw.gameshow)
            mediaPlayer?.isLooping = true
        }

        if (!isMuted) {
            mediaPlayer?.start()
            binding.muteBtn.setImageResource(R.drawable.volume)
        } else {
            binding.muteBtn.setImageResource(R.drawable.mute)
        }
    }

    private fun toggleMute() {
        isMuted = !isMuted

        val editor = sharedPreferences.edit()
        editor.putBoolean("isMuted", isMuted)
        editor.apply()

        if (isMuted) {
            if (mediaPlayer?.isPlaying == true) {
                mediaPlayer?.pause()
            }
            binding.muteBtn.setImageResource(R.drawable.mute)
        } else {
            mediaPlayer?.start()
            binding.muteBtn.setImageResource(R.drawable.volume)
        }
    }

    override fun onPause() {
        super.onPause()
        if (mediaPlayer?.isPlaying == true) {
            mediaPlayer?.pause()
        }
    }

    override fun onResume() {
        super.onResume()
        if (!isMuted) {
            mediaPlayer?.start()
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        mediaPlayer?.stop()
        mediaPlayer?.release()
        mediaPlayer = null
    }
}