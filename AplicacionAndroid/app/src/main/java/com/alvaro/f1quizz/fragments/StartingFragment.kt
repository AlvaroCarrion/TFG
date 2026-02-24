package com.alvaro.f1quizz.fragments

import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.navigation.NavController
import androidx.navigation.Navigation
import com.alvaro.f1quizz.R
import com.alvaro.f1quizz.databinding.FragmentStartingBinding
import com.bumptech.glide.Glide
import com.google.firebase.auth.FirebaseAuth

class StartingFragment : Fragment() {
    private var _binding: FragmentStartingBinding? = null
    private val binding get() = _binding!!

    private lateinit var mAuth: FirebaseAuth
    private lateinit var navController: NavController

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentStartingBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        init(view)

        Glide.with(this)
            .asGif()
            .load(R.drawable.tyre)
            .into(binding.imageView3)

        val isLogin: Boolean = mAuth.currentUser != null

        val handler = Handler(Looper.myLooper()!!)
        handler.postDelayed({
            if (isLogin)
                navController.navigate(R.id.action_startingFragment_to_homeFragment)
            else
                navController.navigate(R.id.action_startingFragment_to_logInFragment)
        }, 3000)
    }

    private fun init(view: View) {
        mAuth = FirebaseAuth.getInstance()
        navController = Navigation.findNavController(view)
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}