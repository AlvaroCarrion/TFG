package com.alvaro.f1quizz.fragments

import android.os.Parcel
import android.os.Parcelable

class QuestionResult() : Parcelable {
    var question: String = ""
    var isCorrect: Boolean = false
    var points: Int = 0

    // Constructor secundario para crear el objeto normalmente.
    constructor(question: String, isCorrect: Boolean, points: Int) : this() {
        this.question = question
        this.isCorrect = isCorrect
        this.points = points
    }

    // Constructor usado por Parcel para reconstruir el objeto.
    constructor(parcel: Parcel) : this() {
        question = parcel.readString() ?: ""
        isCorrect = parcel.readByte() != 0.toByte()
        points = parcel.readInt()
    }

    override fun writeToParcel(parcel: Parcel, flags: Int) {
        parcel.writeString(question)
        parcel.writeByte(if (isCorrect) 1 else 0)
        parcel.writeInt(points)
    }

    override fun describeContents(): Int = 0

    companion object CREATOR : Parcelable.Creator<QuestionResult> {
        override fun createFromParcel(parcel: Parcel): QuestionResult {
            return QuestionResult(parcel)
        }

        override fun newArray(size: Int): Array<QuestionResult?> {
            return arrayOfNulls(size)
        }
    }
}