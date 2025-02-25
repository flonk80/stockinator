import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import yfinance as yf
from sklearn.preprocessing import MinMaxScaler
import tensorflow as tf
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense, Dropout

# Define the stock ticker and timeframe
ticker = "AAPL"

# Fetch historical market data
stock_data = yf.download(ticker, period="6mo", interval="1d")

# Select relevant columns
features = ['Open', 'High', 'Low', 'Close', 'Volume']
df = stock_data[features]

# Normalize data
scaler = MinMaxScaler()
df_scaled = scaler.fit_transform(df)

# Convert to sequences
lookback = 30
X, y = [], []

for i in range(lookback, len(df_scaled)):
    X.append(df_scaled[i - lookback:i])
    y.append(df_scaled[i, 3])  # Predicting 'Close' price

X, y = np.array(X), np.array(y)

print("Total sequences created:", len(X))
print(y, X)

# Split into train and test sets
train_size = int(len(X) * 0.8)
X_train, X_test, y_train, y_test = X[:train_size], X[train_size:], y[:train_size], y[train_size:]



# Build LSTM model
model = Sequential([
    LSTM(50, return_sequences=True, input_shape=(lookback, len(features))),
    LSTM(50, return_sequences=False),
    Dense(25, activation="relu"),
    Dense(1)  # Output layer
])

model.compile(optimizer="adam", loss="mse")

# Train model
model.fit(X_train, y_train, epochs=10, batch_size=16, validation_data=(X_test, y_test))

# Make predictions
predictions = model.predict(X_test)
print(predictions)
# Plot results
plt.figure(figsize=(12, 6))
plt.plot(y_test, label="Actual Prices")
plt.plot(predictions, label="Predicted Prices")
plt.legend()
plt.show()