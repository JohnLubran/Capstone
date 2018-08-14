CD C:\Users\JohnLubran\Source\Repos\MyFirstProject\Capstone\Data\ffmpeg\bin
PROMPT $P$_$G
SET PATH=%CD%;%PATH%
ffmpeg -i C:\Users\JohnLubran\Source\Repos\MyFirstProject\Capstone\Data\00115.MTS -f image2 -ss 11 -to 12 C:\Users\JohnLubran\Source\Repos\MyFirstProject\Capstone\Data\frameA-%%09d.bmp
exit
