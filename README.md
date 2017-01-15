# GmailProxy

Simple prototype of an API proxy exposing POP3/SMTP connectivity to a local mail client (like Outlook, Thunderbird, etc) and translated into Google API RASTful calls to GMail service.

## Motivation
Using GmailProxy you have more handle on how you want to represent GMail mailbox to a mail client than using out of the box GMail POP3/SMTP protocol

## Authors

* **Sergei Belenki** - *Initial work* - [BaoBau](www2.baobau.com.s3-website-us-east-1.amazonaws.com)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

[LumiSoft.NET](http://www.lumisoft.ee/lsWWW/ENG/index_eng.aspx?type=main) library was used as an examle of POP3/SMTP server for integration, you can use any other POP3/SMTP server implemntation although the events model would be different.