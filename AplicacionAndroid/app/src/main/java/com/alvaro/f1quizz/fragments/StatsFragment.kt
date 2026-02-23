package com.alvaro.f1quizz.fragments

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.navigation.NavController
import androidx.navigation.Navigation
import androidx.navigation.fragment.findNavController
import com.alvaro.f1quizz.R
import com.alvaro.f1quizz.databinding.FragmentStatsBinding
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.database.*

class StatsFragment : Fragment() {
    private lateinit var binding: FragmentStatsBinding
    private val database = FirebaseDatabase.getInstance().reference
    private lateinit var navController: NavController

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        binding = FragmentStatsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        navController = Navigation.findNavController(view)

        val userId = FirebaseAuth.getInstance().currentUser?.uid
        if (userId != null) {
            loadUserStats(userId)
        }

        binding.homeBtn.setOnClickListener {
            navController.navigate(R.id.action_statsFragment_to_homeFragment)
        }
    }

    private fun loadUserStats(userId: String) {
        database.child("UserData").child(userId).addListenerForSingleValueEvent(object : ValueEventListener {
            override fun onDataChange(snapshot: DataSnapshot) {
                val stats = snapshot.getValue(UserStats::class.java)
                if (stats != null) {
                    binding.tvTotalScore.text = "${getString(R.string.total_score)}: ${stats.totalScore}"
                    binding.tvQuizzesPlayed.text = "${getString(R.string.total_quizzes)}: ${stats.totalQuizzes}"

                    // NUEVA LÍNEA: Mostrar total de preguntas
                    binding.tvTotalAnswers.text = "${getString(R.string.total_questions)}: ${stats.totalAnswers}"

                    binding.tvCorrectAnswers.text = "${getString(R.string.incorrect_answers)}: ${stats.totalCorrectAnswers}"
                    binding.tvWrongAnswers.text = "${getString(R.string.correct_answers)}: ${stats.totalWrongAnswers}"
                }
            }
            override fun onCancelled(error: DatabaseError) {
            }
        })
    }
}