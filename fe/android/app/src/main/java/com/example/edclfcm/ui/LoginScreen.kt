package com.example.edclfcm.ui

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import com.example.edclfcm.api.ApiClient
import com.example.edclfcm.api.LoginRequest
import com.example.edclfcm.util.TokenManager
import kotlinx.coroutines.launch
import androidx.compose.ui.platform.LocalContext

@Composable
fun LoginScreen(onLoginSuccess: () -> Unit) {
    val context = LocalContext.current
    val coroutineScope = rememberCoroutineScope()
    var phone by remember { mutableStateOf("") }
    var pin by remember { mutableStateOf("") }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf("") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(text = "EDCL FCM Simulator", style = MaterialTheme.typography.headlineMedium)
        Spacer(modifier = Modifier.height(32.dp))

        OutlinedTextField(
            value = phone,
            onValueChange = { phone = it },
            label = { Text("Phone Number") },
            modifier = Modifier.fillMaxWidth()
        )
        Spacer(modifier = Modifier.height(16.dp))
        
        OutlinedTextField(
            value = pin,
            onValueChange = { pin = it },
            label = { Text("PIN") },
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
                if (phone.isBlank() || pin.isBlank()) {
                    errorMessage = "Please enter phone and PIN"
                    return@Button
                }
                isLoading = true
                errorMessage = ""
                coroutineScope.launch {
                    try {
                        val response = ApiClient.service.login(LoginRequest(phone, pin))
                        if (response.isSuccessful && response.body()?.isSuccess == true) {
                            val data = response.body()?.data
                            data?.accessToken?.let {
                                TokenManager(context).saveAccessToken(it)
                            }
                            onLoginSuccess()
                        } else {
                            errorMessage = response.body()?.message ?: "Login failed"
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
                Text("Login")
            }
        }
    }
}
