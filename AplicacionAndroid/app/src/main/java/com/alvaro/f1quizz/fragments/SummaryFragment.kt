package com.alvaro.f1quizz.fragments

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.navigation.fragment.findNavController
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.alvaro.f1quizz.R
import com.alvaro.f1quizz.databinding.FragmentSummaryBinding

class SummaryFragment : Fragment() {

    private var _binding: FragmentSummaryBinding? = null
    private val binding get() = _binding!!
    private var resultsList: ArrayList<QuestionResult>? = null

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentSummaryBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        // Recuperar la lista enviada desde QuizFragment.
        resultsList = arguments?.getParcelableArrayList("RESULTS_LIST")

        setupRecyclerView()

        // Calcular puntuación total del quiz actual.
        val totalPoints = resultsList?.sumOf { it.points } ?: 0
        binding.tvTotalScore.text = "${getString(R.string.total_score)}: $totalPoints"

        binding.btnBackToHome.setOnClickListener {
            findNavController().navigate(R.id.action_summaryFragment_to_homeFragment)
        }
    }

    private fun setupRecyclerView() {
        binding.rvSummary.layoutManager = LinearLayoutManager(context)
        binding.rvSummary.adapter = SummaryAdapter(resultsList ?: arrayListOf())
    }

    inner class SummaryAdapter(private val list: List<QuestionResult>) : RecyclerView.Adapter<SummaryAdapter.ViewHolder>() {

        inner class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
            val tvNum: TextView = view.findViewById(R.id.tvQuestionNum)
            val tvQ: TextView = view.findViewById(R.id.tvQuestionText)
            val tvStatus: TextView = view.findViewById(R.id.tvStatus)
            val tvPts: TextView = view.findViewById(R.id.tvPoints)
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
            val view = LayoutInflater.from(parent.context).inflate(R.layout.item_quiz_result, parent, false)
            return ViewHolder(view)
        }

        override fun onBindViewHolder(holder: ViewHolder, position: Int) {
            val item = list[position]
            holder.tvNum.text = "${getString(R.string.question_label)} ${position + 1}"
            holder.tvQ.text = item.question

            if (item.isCorrect) {
                holder.tvStatus.text = getString(R.string.correct_label)
                holder.tvStatus.setTextColor(ContextCompat.getColor(requireContext(), android.R.color.holo_green_dark))
            } else {
                holder.tvStatus.text = getString(R.string.incorrect_label)
                holder.tvStatus.setTextColor(ContextCompat.getColor(requireContext(), R.color.f1Red))
            }

            holder.tvPts.text = "${item.points} pts"
        }

        override fun getItemCount(): Int = list.size
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}