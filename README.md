# Open-Internet-Watchdog

## What this program do:
This program is a small watchdog that checks your Internet connection. When you go offline, this program notices this and writes this into a file. When you go online, this program also writes into this file, how long you where offline. 
In the future, this program should also run a test, how fast your connection is.

## How this program works:
This program send a Ping to a Domain or IP Address. You might want to use a Domain, if you want to check if your website is online. You need to enter an IP, so that the program checks if the DNS gave the program the right IP Address. Some routers might point to them self, if they lost connection to the internet. 

## How to use this program:
After you downloaded the latest [release](https://github.com/Hundhausen/Open-Internet-Watchdog/releases)
you get a .zip file that you extract. You run the Setup file and then the program starts. The initial setup should be easy. The program closes, when you press any key, after the initial setup. When you said no to the debug question, the program will run hidden, after the next startup. 
When you want to access the files, start the program, look into the Task-Manager and find “Open Internet Watchdog.exe”. With a right click, you can click the option to open the folder, where the .exe file is located. This will get changed in a later release. 
The connections.csv file is straightforward.  Before the semicolon (;) is the domain and on the right side is the IP. You don’t need a Domain.

The Config.xml has comments in it and should also be easy to edit. 

I recommend to use multiply IP Addresses that are definitely online. We don’t provide any IP Addresses, because I don’t want to be responsible for any spam. 

```diff
- IMPORTANT: I am not responsible for any spam or that you get blocked. I recommend to check for a connection every 300 Seconds, that are equal to 5 Minutes. With this timespan you can check if you are connected to the internet and don’t generate any significant traffic to other sites.
```

## What might get changed or added:
* A better Setup, where you can choose the install location
* Adding a speedtest (I want to have this but I still looking for an external service that work for everyone and provide good results, as good as possible)
* Priorities with the connections to test so that you have some that you always want to check and with the others, the program picks a random IP/Domain to check in this priority  level
* Polishing the code. The code is pretty dirty at this point
* Better console messages
