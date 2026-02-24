package com.alvaro.f1quizz.fragments

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.navigation.NavController
import androidx.navigation.Navigation
import com.google.firebase.auth.FirebaseAuth
import com.alvaro.f1quizz.databinding.FragmentRegisterBinding
import com.alvaro.f1quizz.R
import com.alvaro.f1quizz.utils.Notifications

class RegisterFragment : Fragment() {
    private lateinit var navController: NavController
    private lateinit var mAuth: FirebaseAuth
    private lateinit var binding: FragmentRegisterBinding

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        binding = FragmentRegisterBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        init(view)

        binding.textViewSignIn.setOnClickListener {
            navController.navigate(R.id.action_registerFragment_to_logInFragment)
        }

        binding.nextBtn.setOnClickListener {
            val email = binding.emailEt.text.toString().trim()
            val pass = binding.passEt.text.toString()
            val verifyPass = binding.verifyPassEt.text.toString()

            if (email.isEmpty() || pass.isEmpty() || verifyPass.isEmpty()) {
                Toast.makeText(context, getString(R.string.empty_fields), Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            if (!android.util.Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
                Toast.makeText(context, getString(R.string.invalid_mail), Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            if (pass.length < 8) {
                Toast.makeText(context, getString(R.string.short_pass), Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            if (!pass.contains(Regex("[A-Z]"))) {
                Toast.makeText(context, getString(R.string.capital_pass), Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            if (pass != verifyPass) {
                Toast.makeText(context, getString(R.string.equal_pass), Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }
            registerUser(email, pass)
        }
    }

    private fun registerUser(email: String, pass: String) {
        mAuth.createUserWithEmailAndPassword(email, pass).addOnCompleteListener { task ->
            if (task.isSuccessful) {
                Notifications.showNotification(requireContext(), getString(R.string.notification_register_title), getString(R.string.notification_register_msg))

                navController.navigate(R.id.action_registerFragment_to_homeFragment)
            } else {
                val exception = task.exception
                if (exception is com.google.firebase.auth.FirebaseAuthUserCollisionException) {
                    Toast.makeText(context, getString(R.string.existing_account), Toast.LENGTH_SHORT).show()
                } else {
                    Toast.makeText(context, "${getString(R.string.error_at_register)}: ${exception?.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    private fun init(view: View) {
        navController = Navigation.findNavController(view)
        mAuth = FirebaseAuth.getInstance()
    }
}