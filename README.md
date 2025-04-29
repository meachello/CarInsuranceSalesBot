# 🚗 Telegram Bot for Car Insurance Sales

This project is a Telegram Bot built with C# that guides users through the process of purchasing car insurance. The bot interacts with users using text messages, processes document submissions, and issues a dummy insurance policy based on provided information.

## Link to the telegram bot
https://t.me/TestInsuranceSalesBot


## 📋 Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Setup Instructions](#setup-instructions)
- [Bot Workflow](#bot-workflow)
- [Interaction Examples](#interaction-examples)

## ✅ Features

- Telegram bot initialization and command handling

- Photo submission and validation for:

    Passport

    Vehicle Identification Document

- Document data extraction via Mindee API (mock)

- AI-powered confirmation dialogs using OpenAI

- Price agreement logic (fixed at 100 USD)

- Dummy insurance policy generation and delivery


## 🧩 Requirements
.NET 6.0+

Telegram Bot API token

(Optional) Mindee API key

(Optional) OpenAI API key



## ⚙️ Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/meachello/CarInsuranceSalesBot.git
cd CarInsuranceSalesBot
```
### 2. Configure environment variables (optional)
Create a .env or use appsettings.json for:

- Mindee API Key (optional)

- OpenAI API Key (optional)

### 3. Restore dependencies and run

```bash
dotnet restore
dotnet run
```


## 🔄 Bot Workflow
### Introduction
User starts the bot with /start. The bot introduces itself and explains its function.

### Document Submission
User uploads:

Passport photo

Vehicle identification document photo

### Data Extraction
Bot sends documents to Mindee API (mock), extracts relevant fields (name, ID number, car details, etc.), and displays them to the user for confirmation.

### Data Confirmation Loop
If data is incorrect:

User is prompted to resubmit the photo

Extraction and confirmation are repeated

### Price Quotation
Bot informs user of a fixed $100 price. If the user declines, it politely explains that the price is non-negotiable.

### Policy Generation
If user agrees:

Bot uses OpenAI to generate a dummy policy document (not free) or generates a simple text file

Sends it to the user via Telegram

## 💬 Interaction Examples
```vbnet
User: /start  
Bot: 👋 Welcome to our Car Insurance Bot! I'm here to help you quickly purchase car insurance. I'll guide you through document submission, verification, and policy issuance.
To get started, please send me a clear photo of your passport. Make sure all text is readable and the entire document is visible.

User sends photo

Bot: Thank you for sending your passport photo. Now, please send a photo of your vehicle identification document.

User sends photo

Bot: Thank you for submitting both documents. I'm now processing them...
I've extracted the following information from your documents:

👤 Full Name: John Smith
🎂 Date of Birth: 15-05-1985
🆔 Passport Number: AB123456
🚗 Vehicle: Toyota Camry (2020)
🔢 License Plate: XYZ789

Is this information correct? Please reply with 'yes' or 'no'.

User: Yes

Bot: Based on the information provided, your car insurance premium is 100 USD. Do you agree with this price and wish to proceed with the purchase?

User: Yes

Bot: Thank you for your purchase! Your insurance policy has been generated successfully. Here is your policy document:
**document**
Thank you for choosing our insurance services! Your policy is now active. If you have any questions or need assistance, feel free to contact our support team. Drive safely!
```
