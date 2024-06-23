# description

dmd-play is a client that connect on the server to load images (png and animatd gif) or videos or texts.

dmd-play communicates via a tcp connexion.


# play an image from the client
<code>DmdPlay -f "file.png"
DmdPlay -f "file.gif"
DmdPlay -v "file.mp4"
DmdPlay -t "Hello world"
DmdPlay --help

options:
  -h, --help            show this help message and exit
  -f FILE, --file FILE
  -v VIDEO, --video VIDEO
  -t TEXT, --text TEXT
  --font FONT           path to the font file
  --clear               clear the screen
  --overlay             restore the previous frames once finished
  --overlay-time OVERLAY_TIME
                        time to pause fixed images for the overlay in ms
  --moving-text         always makes the text to move, even if text fits
  --fixed-text          never makes the text to move, prefer to adjust size
  -r RED, --red RED     red text color
  -g GREEN, --green GREEN
                        green text color
  -b BLUE, --blue BLUE  blue text color
  -s SPEED, --speed SPEED
                        sleep time during each text position (in milliseconds)
  -m MOVE, --move MOVE  text movement each time
  --once                don't loop forever
  -p PORT, --port PORT  network connexion port
  --host HOST           dmd server host
  --width WIDTH         dmd width
  --height HEIGHT       dmd height
</code>

