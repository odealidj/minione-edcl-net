package com.example.edclfcm.ui

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import com.example.edclfcm.api.ApiClient
import com.example.edclfcm.api.ChangePinRequest
import com.example.edclfcm.util.TokenManager
import kotlinx.coroutines.launch
import androidx.compose.ui.platform.LocalContext

@Composable
fun ChangePinScreen(phone: String, oldPin: String, onChangeSuccess: () -> Unit) {
    val context = LocalContext.current
    val coroutineScope = rememberCoroutineScope()
    var newPin by remember { mutableStateOf("") }
    var confirmPin by remember { mutableStateOf("") }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf("") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(text = "Change Default PIN", style = MaterialTheme.typography.headlineMedium)
        Spacer(modifier = Modifier.height(16.dp))
        Text(text = "For security reasons, you must change your default PIN before continuing.", style = MaterialTheme.typography.bodyMedium)
        Spacer(modifier = Modifier.height(32.dp))

        OutlinedTextField(
            value = newPin,
            onValueChange = { newPin = it },
            label = { Text("New PIN") },
            visualTransformation = PasswordVisualTransformation(),
            modifier = Modifier.fillMaxWidth()
        )
        Spacer(modifier = Modifier.height(16.dp))
        
        OutlinedTextField(
            value = confirmPin,
            onValueChange = { confirmPin = it },
            label = { Text("Confirm New PIN") },
            visualTransformation = PasswordVisualTransformation(),
            modifier = Modifier.fillMaxWidth()
        )
        Spacer(modifier = Modifier.height(32.dp))

        if (errorMessage.isNotEmpty()) {
            Text(text = errorMessage, color = MaterialTheme.colorScheme.error)
            Spacer(modifier = Modifier.height(16.dp))
        }

        Button(
            onClick = {
                if (newPin.isBlank() || confirmPin.isBlank()) {
                    errorMessage = "Please enter and confirm your new PIN"
                    return@Button
                }
                if (newPin != confirmPin) {
                    errorMessage = "PINs do not match"
                    return@Button
                }

                isLoading = true
                errorMessage = ""
                coroutineScope.launch {
                    try {
                        val response = ApiClient.service.changePin(
                            ChangePinRequest(
                                phoneNumber = phone,
                                oldPin = oldPin,
                                newPin = newPin
                            )
                        )
                        if (response.isSuccessful && response.body()?.status == "success") {
                            val data = response.body()?.data
                            data?.accessToken?.let {
                                TokenManager(context).saveAccessToken(it)
                            }
                            onChangeSuccess()
                        } else {
                            errorMessage = response.body()?.message ?: "Change PIN failed"
                        }
                    } catch (e: Exception) {
                        errorMessage = e.localizedMessage ?: "Unknown error"
                    } finally {
                        isLoading = false
                    }
                }
            },
            modifier = Modifier.fillMaxWidth(),
            enabled = !isLoading
        ) {
            if (isLoading) {
                CircularProgressIndicator(modifier = Modifier.size(24.dp))
            } else {
                Text("Update PIN & Login")
            }
        }
    }
}
