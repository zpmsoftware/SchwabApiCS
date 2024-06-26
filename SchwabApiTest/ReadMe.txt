Schwab API Test.

To start, you need to modify SchwabTokens.json  (located in the SchwabApiCS_Test folder)

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

I'm pretty new to using async/await and all seems to be working, but if I'm doing something wrong let me know.

The tokens, appkey and secret are stored in a plain text file (SchwabTokens.json).
Some thought should be given to where it should be kept and encryption.

If we get a handful of people interested, we may want to set up a github site.

Always open to suggestions, because I don't know what I don't know, and don't mind learning.
And of course, there's always bugs...

Also, I expect you will find some issues getting started.  Let me know about them so I can address it for the next person.

See ReleaseNotes.txt in the SchwabApiCs project.

FYI - ZPM Software Inc is my consulting company, just me.

Gary Hoff.

====== More General Notes =======================================
Double vs Decimal
I'm using decimal instead of double in most api methods. Why? No rounding issues with decimal.
However with PriceHistory I use double. Why? Performance. Decimal is slower than double. 
Immaterial most of the time, but with price history I do a lot of back testing, often millions of calculations.

If you really want to use double, I don't see any issues with a global replace all decimal with double (but haven't tried it).


