# GmailProxy

Simple prototype of an API proxy exposing POP3/SMTP connectivity to a local mail client (like Outlook, Thunderbird, etc) and translated into Google API RESTful calls to Gmail service.

## Motivation
Using GmailProxy you have more handle on how you want to represent Gmail mailbox to a mail client than using out of the box Gmail POP3/SMTP protocol

## Authors

* **Sergei Belenki** - *Initial work* - [BaoBau](http://www2.baobau.com.s3-website-us-east-1.amazonaws.com)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

[LumiSoft.NET](http://www.lumisoft.ee/lsWWW/ENG/Products/Mail_Server/mail_index_eng.aspx?type=info) library was used as an example of POP3/SMTP server for integration, you can use any other POP3/SMTP server implementation although the events model would be different.
