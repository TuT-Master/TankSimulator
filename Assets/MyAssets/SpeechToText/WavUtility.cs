using UnityEngine;
using System;
using System.IO;
using System.Text;

public static class WavUtility
{
    const int HEADER_SIZE = 44;

    public static void SaveWav(string filePath, AudioClip clip)
    {
        if (!filePath.ToLower().EndsWith(".wav"))
            filePath += ".wav";

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            int samples = clip.samples * clip.channels;
            float[] data = new float[samples];
            clip.GetData(data, 0);

            byte[] bytes = ConvertAudioClipDataToInt16ByteArray(data);

            WriteHeader(fileStream, clip, bytes.Length);
            fileStream.Write(bytes, 0, bytes.Length);
        }
    }

    private static byte[] ConvertAudioClipDataToInt16ByteArray(float[] data)
    {
        Int16[] intData = new Int16[data.Length];
        byte[] bytesData = new byte[data.Length * 2];

        for (int i = 0; i < data.Length; i++)
        {
            intData[i] = (short)(data[i] * short.MaxValue);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        return bytesData;
    }

    private static void WriteHeader(FileStream stream, AudioClip clip, int dataLength)
    {
        int sampleRate = clip.frequency;
        int channels = clip.channels;
        int byteRate = sampleRate * channels * 2;

        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(dataLength + HEADER_SIZE - 8);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);
        }
    }
}
