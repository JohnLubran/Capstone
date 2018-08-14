CD C:\Users\JohnLubran\Source\Repos\MyFirstProject\Capstone\Data\ffmpeg\bin
PROMPT $P$_$G
SET PATH=%CD%;%PATH%
ffmpeg -i C:\Users\JohnLubran\Source\Repos\MyFirstProject\Capstone\Data\Test1.MTS -ss 1 -to 2 -b:v 30000k -r 60 C:\Users\JohnLubran\Source\Repos\MyFirstProject\Capstone\Data\frame.mp4
exit
