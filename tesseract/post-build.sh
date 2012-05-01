#!/bin/sh

cp tesseract-3.01/vs2008/bin/tesseract.exe ./

if [ ! -e ./tessdata ]; then
    mkdir tessdata
fi

if [ ! -e ./tessdata/jpn.traineddata ]; then
    cp jpn.traineddata ./tessdata/
fi
