What it Does
============

This porject contains a worker role that basically performs an export operation (.bacpac) from SQL Azure to blob storage. 
For this, it uses the [Import Export Client Side Tools](http://sqldacexamples.codeplex.com/wikipage?title=Import%20Export%20Client%20Side%20Tools&referringTitle=Home) provided by the DAC team.
It also performs another tasks such as save the Server log to blob storage or send a blob via FTP.