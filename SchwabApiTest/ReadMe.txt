Schwab API Test.

It this is your initial setup, you need to copy SchwabApiCS_Test\SchwabTokens_empty.json 
to SchwabApiCS_Test\SchwabTokens.json before you run SchwabApiCS_Test.

Edit SchwabApiCS_Test\SchwabTokens.json:
1. Set AccessToken to "". This will trigger the app to run the web page to generate new tokens.
2. Set AppKey to your appkey value.
3. Set Secret to your secret value.
4. Set Redirect_uri to your value, usually https://127.0.0.1

Run  SchwabApiCS_Test and it will show the Schwab reauthorization page on startup.
The Schwab reauthorization page will show automatically if the accessToken expires today, or has expired.
Refresh authorizations will happen automatically.

If you get a "Whitelabel Error Page" when the test app starts up, It's likely because your app's status with Schwab is not "Ready For Use".
Go to the Schwab Developer Portal and click on Dashboard and check your app status.  
Of course you have to follow the instructions to create a "app" first.

The top of SchwabApi.cs has a list of all the methods and the endpoints the implement, good reference to review.

I left a break point in the MainWindow.xaml.cs Test() method.  
There you can inspect some of the responses.

See ReleaseNotes.docx in the SchwabApiCS folder for upto date changes.

The tokens, appkey and secret are stored in a plain text file (SchwabTokens.json).
Some thought should be given to where it should be kept and encryption.

