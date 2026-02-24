package com.alvaro.f1quizz.fragments

import android.content.Context
import android.content.SharedPreferences
import android.content.res.ColorStateList
import android.graphics.Color
import android.media.MediaPlayer
import android.os.Bundle
import android.os.CountDownTimer
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.navigation.NavController
import androidx.navigation.Navigation
import androidx.navigation.fragment.findNavController
import com.alvaro.f1quizz.R
import com.alvaro.f1quizz.databinding.FragmentQuizBinding
import com.google.android.material.button.MaterialButton
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.database.*
import java.util.*

class QuizFragment : Fragment() {
    private lateinit var binding: FragmentQuizBinding
    private val database = FirebaseDatabase.getInstance().reference
    private lateinit var navController: NavController

    private var allQuestions = mutableListOf<QuestionModel>()
    private var quizQuestions = mutableListOf<QuestionModel>()
    private val quizResults = mutableListOf<QuestionResult>()

    private var currentQuestionIndex = 0
    private var timer: CountDownTimer? = null
    private val systemLanguage = Locale.getDefault().language
    private var isDataLoaded = false
    private var timeLeft: Long = 0
    private var mediaPlayer: MediaPlayer? = null
    private lateinit var sharedPreferences: SharedPreferences

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        binding = FragmentQuizBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        navController = Navigation.findNavController(view)

        sharedPreferences = requireActivity().getSharedPreferences("AppPreferences", Context.MODE_PRIVATE)

        playMusic()

        binding.tvQuestion.visibility = View.INVISIBLE
        binding.optionsContainer.visibility = View.INVISIBLE
        binding.quizProgressBar.visibility = View.INVISIBLE
        binding.tvTimer.visibility = View.INVISIBLE

        binding.cvCloseQuiz.setOnClickListener {
            stopQuizAndExit()
        }

        loadQuestionsFromFirebase()
    }

    private fun playMusic() {
        mediaPlayer?.release()
        mediaPlayer = null

        val isMuted = sharedPreferences.getBoolean("isMuted", false)
        if (isMuted) return

        try {
            val ctx = context ?: return
            mediaPlayer = MediaPlayer.create(ctx, R.raw.tension)
            mediaPlayer?.apply {
                isLooping = true
                start()
            }
        } catch (e: Exception) {
            Log.e("AudioError", "Error: ${e.message}")
        }
    }

    private fun loadQuestionsFromFirebase() {
        val categories = listOf("questionsCircuits", "questionsDrivers", "questionsTeams")
        var loadedCategories = 0
        categories.forEach { category ->
            database.child(category).addListenerForSingleValueEvent(object : ValueEventListener {
                override fun onDataChange(snapshot: DataSnapshot) {
                    if (snapshot.exists()) {
                        for (data in snapshot.children) {
                            val question = data.getValue(QuestionModel::class.java)
                            question?.let { allQuestions.add(it) }
                        }
                    }
                    loadedCategories++
                    if (loadedCategories == categories.size) {
                        isDataLoaded = true
                        startPreparationTimer()
                    }
                }
                override fun onCancelled(error: DatabaseError) {
                    Log.e("FirebaseQuiz", error.message)
                }
            })
        }
    }

    private fun stopQuizAndExit() {
        timer?.cancel()
        timer = null
        Toast.makeText(context, getString(R.string.canceled_quiz), Toast.LENGTH_SHORT).show()
        navController.navigate(R.id.action_quizFragment_to_homeFragment)
    }

    private fun startPreparationTimer() {
        binding.tvTimer.visibility = View.VISIBLE
        if (allQuestions.isEmpty()) {
            Toast.makeText(context, getString(R.string.load_error), Toast.LENGTH_SHORT).show()
            return
        }
        quizQuestions = allQuestions.shuffled().take(15).toMutableList()
        binding.quizProgressBar.max = quizQuestions.size
        timer?.cancel()
        timer = object : CountDownTimer(5000, 1000) {
            override fun onTick(millisUntilFinished: Long) {
                binding.tvTimer.text = (millisUntilFinished / 1000 + 1).toString()
            }
            override fun onFinish() {
                binding.tvQuestion.visibility = View.VISIBLE
                binding.optionsContainer.visibility = View.VISIBLE
                binding.quizProgressBar.visibility = View.VISIBLE
                showQuestion()
            }
        }.start()
    }

    private fun showQuestion() {
        if (!isAdded) return
        if (currentQuestionIndex >= 15) {
            saveStatsAndGoToSummary()
            return
        }
        val question = quizQuestions[currentQuestionIndex]
        val lang = if (systemLanguage == "es") "sp" else "en"
        val data = if (lang == "sp") question.language.sp else question.language.en

        binding.tvQuestion.text = data.question
        binding.quizProgressBar.progress = currentQuestionIndex + 1

        val buttons = listOf(binding.btnOpt1, binding.btnOpt2, binding.btnOpt3, binding.btnOpt4)
        val shuffledOptions = data.options.shuffled()

        buttons.forEachIndexed { index, button ->
            button.text = shuffledOptions[index]
            button.isEnabled = true
            button.setTextColor(ContextCompat.getColor(requireContext(), R.color.black))
            button.strokeColor = ColorStateList.valueOf(Color.parseColor("#BDBDBD"))
            button.backgroundTintList = ColorStateList.valueOf(Color.TRANSPARENT)
            button.setOnClickListener {
                checkAnswer(button, shuffledOptions[index], data.answer, data.question)
            }
        }
        resetAndStartTimer()
    }

    private fun checkAnswer(selectedButton: MaterialButton, selectedOption: String, correctAnswer: String, questionText: String) {
        timer?.cancel()
        val buttons = listOf(binding.btnOpt1, binding.btnOpt2, binding.btnOpt3, binding.btnOpt4)
        buttons.forEach { it.isEnabled = false }

        val isCorrect = selectedOption == correctAnswer
        val points = if (isCorrect) calculatePoints() else -50

        if (isCorrect) {
            selectedButton.backgroundTintList = ColorStateList.valueOf(Color.GREEN)
        } else {
            selectedButton.backgroundTintList = ColorStateList.valueOf(Color.RED)
            buttons.find { it.text == correctAnswer }?.backgroundTintList = ColorStateList.valueOf(Color.GREEN)
        }

        quizResults.add(QuestionResult(questionText, isCorrect, points))
        Handler(Looper.getMainLooper()).postDelayed({
            currentQuestionIndex++
            showQuestion()
        }, 1500)
    }

    private fun calculatePoints(): Int {
        val seconds = timeLeft / 1000
        return when {
            seconds >= 10 -> 150
            seconds >= 5 -> 100
            else -> 50
        }
    }

    private fun resetAndStartTimer() {
        timer?.cancel()
        timer = object : CountDownTimer(15000, 1000) {
            override fun onTick(millisUntilFinished: Long) {
                timeLeft = millisUntilFinished
                binding.tvTimer.text = (millisUntilFinished / 1000).toString()
            }
            override fun onFinish() {
                val buttons = listOf(binding.btnOpt1, binding.btnOpt2, binding.btnOpt3, binding.btnOpt4)
                buttons.forEach { it.isEnabled = false }
                val question = quizQuestions[currentQuestionIndex]
                val lang = if (systemLanguage == "es") "sp" else "en"
                val data = if (lang == "sp") question.language.sp else question.language.en
                buttons.find { it.text == data.answer }?.backgroundTintList = ColorStateList.valueOf(Color.GREEN)
                quizResults.add(QuestionResult(data.question, false, -50))
                Handler(Looper.getMainLooper()).postDelayed({
                    currentQuestionIndex++
                    showQuestion()
                }, 2000)
            }
        }.start()
    }

    private fun saveStatsAndGoToSummary() {
        val user = FirebaseAuth.getInstance().currentUser
        val userId = user?.uid ?: return
        val totalPointsEarned = quizResults.sumOf { it.points }
        val correctCount = quizResults.count { it.isCorrect }
        val wrongCount = quizResults.size - correctCount

        database.child("UserData").child(userId).runTransaction(object : Transaction.Handler {
            override fun doTransaction(mutableData: MutableData): Transaction.Result {
                var stats = mutableData.getValue(UserStats::class.java)
                if (stats == null) {
                    stats = UserStats(totalPointsEarned, 1, 15, correctCount, wrongCount)
                } else {
                    stats.totalScore += totalPointsEarned
                    stats.totalQuizzes += 1
                    stats.totalAnswers += 15
                    stats.totalCorrectAnswers += correctCount
                    stats.totalWrongAnswers += wrongCount
                }
                mutableData.value = stats
                return Transaction.success(mutableData)
            }
            override fun onComplete(error: DatabaseError?, committed: Boolean, snapshot: DataSnapshot?) {
                val bundle = Bundle().apply {
                    putParcelableArrayList("RESULTS_LIST", ArrayList(quizResults))
                }
                findNavController().navigate(R.id.action_quizFragment_to_summaryFragment, bundle)
            }
        })
    }

    override fun onPause() {
        super.onPause()
        mediaPlayer?.pause()
    }

    override fun onResume() {
        super.onResume()
        val isMuted = sharedPreferences.getBoolean("isMuted", false)
        if (!isMuted) {
            mediaPlayer?.start()
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        timer?.cancel()
        mediaPlayer?.stop()
        mediaPlayer?.release()
        mediaPlayer = null
    }
}

data class UserStats(
    var totalScore: Int = 0,
    var totalQuizzes: Int = 0,
    var totalAnswers: Int = 0,
    var totalCorrectAnswers: Int = 0,
    var totalWrongAnswers: Int = 0
)

data class QuestionModel(val language: LanguageData = LanguageData())
data class LanguageData(val sp: QuestionContent = QuestionContent(), val en: QuestionContent = QuestionContent())
data class QuestionContent(val question: String = "", val options: List<String> = listOf(), val answer: String = "")