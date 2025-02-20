import argparse
import os, os.path
import zipfile
import io


def readui32(file):
    bytes = file.read(4)
    number = bytes[0]
    number += bytes[1] << 8
    number += bytes[2] << 16
    number += bytes[3] << 24
    return number


def skip2zip(file):
    SIGNATURE = b'PK\x05\x06'

    # retrieve the file size
    file.seek(0, 2)
    filesize = file.tell()

    # scan the last 65k (2^16) for the zip signature
    signature_position = filesize
    while signature_position > filesize - (2 << 16):
        file.seek(signature_position, 0)
        data = file.read(len(SIGNATURE))
        if data == SIGNATURE:
            break
        signature_position -= 1
    else:
        raise ValueError("Corrupted zip archive.")

    # skip 8 bytes
    file.seek(8, 1)

    # read size and offset of central directory
    size = readui32(file)
    offset = readui32(file)

    # Calculate beginning of the zip file:
    # There is a "central directory" with the size 'size' located at 'offset' (relative to the zip
    # file). The signature is appended directly after the central directory. We have already found
    # the signature start and know the size of the central directory, so we can calculate the
    # beginning of the central directory via 'signature_position - size'. The result is the "real"
    # offset inside the packed executable. The supposed offset inside the zip file is stored at
    # 'offset', so we can calculate the beginning of the zip-file.
    start = (signature_position - size) - offset

    # seek to the beginning position
    file.seek(start, 0)


def unpack(executablename, unzipdestination=None, lovefilename=True):
    with open(executablename, 'rb') as executable:
        skip2zip(executable)

        data = executable.read()

    if lovefilename:
        with open(lovefilename, 'wb') as lovefile:
            lovefile.write(data)

    if unzipdestination:
        if not os.path.isdir(unzipdestination):
            os.makedirs(unzipdestination)

        zipdata = io.BytesIO(data)
        with zipfile.ZipFile(zipdata, 'r') as zip:
            zip.extractall(unzipdestination)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Unpack a love game executable which has been " \
                                                 "fused by appending the .love file to a love binary. Choose one of two modes: either " \
                                                 "just separate the .love file from the binary, using the '--love' (or '-l') option, or " \
                                                 "extract the contained files into a directory specified with '--extract' (or '-x'). You " \
                                                 "can also specify both options.")
    parser.add_argument('-x', '--extract', metavar="UNZIPDESTINATION", type=str, help="Unzip " \
                                                                                      "files to this folder (it will be created if it doesn't exist, otherwise the contents " \
                                                                                      "will be overwritten!)")
    parser.add_argument('-l', '--love', metavar="LOVEFILE", type=str, help="Split off the .love " \
                                                                           "file and save it to this file (it will be overwritten if it already exists!)")
    parser.add_argument('executable', metavar="EXECUTABLE", type=str, help="The love game " \
                                                                           "executable to unpack.")

    args = parser.parse_args()

    unpack(args.executable, unzipdestination=args.extract, lovefilename=args.love)