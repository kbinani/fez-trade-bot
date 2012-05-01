#!/bin/sh
if [ ! -e tesseract-3.01.tar.gz ]; then
    curl "http://tesseract-ocr.googlecode.com/files/tesseract-3.01.tar.gz" > "tesseract-3.01.tar.gz"
    tar zxvf tesseract-3.01.tar.gz
fi

if [ ! -e tesseract-3.01-win_vs.zip ]; then
    curl "http://tesseract-ocr.googlecode.com/files/tesseract-3.01-win_vs.zip" > tesseract-3.01-win_vs.zip
    unzip tesseract-3.01-win_vs.zip
fi

if [ ! -e jpn.traineddata.gz ]; then
    curl "http://tesseract-ocr.googlecode.com/files/jpn.traineddata.gz" > jpn.traineddata.gz
    gunzip jpn.traineddata.gz
fi

cd ./tesseract-3.01/api && patch < ../../tesseractmain.cpp.patch
