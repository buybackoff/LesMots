Les Mots
========

A playground for different NLP stuff

Stats projects
-----------

* Calculate the most frequent char tuples, triples and qudruples in a corpus of c.7k news about S&P500
companies
* Generate hard-coded dictionaries with the most frequent ones

Hashing projects
-----------
WIP
The goal is to create a 1-to-1 word 64-bit hash using ideas from http://en.wikipedia.org/wiki/Dictionary_coder
and the disctionaries from the LesMots.Stats project.

64 bits from left to right:
1 bit: 0 - if the algo worked for a word, 1 - need a lookup table

each 9 bits next:
2 bits: 00 - single, 01 - tuple, 10 - triple, 11 - quad 
7 bits - value
