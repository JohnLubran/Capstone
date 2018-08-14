CD D:\Documents\Homework\Capstone\MyFirstProject\Capstone\Data\ffmpeg\bin
PROMPT $P$_$G
SET PATH=%CD%;%PATH%
ffmpeg -i D:\Documents\Homework\Capstone\MyFirstProject\Capstone\Data\00101.MTS -f image2 -ss 0 -to 1 D:\Documents\Homework\Capstone\MyFirstProject\Capstone\Data\frameB-%%09d.bmp
exit
