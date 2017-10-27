using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

public class AquesTalk10SamplePlayer : MonoBehaviour
{

	//再生用オーディオソース
	public AudioSource audio_;

	// 声質パラメータ
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct AQTK_VOICE
	{
		public int bas;    // 基本素片 F1E/F2E/M1E (0/1/2)
		public int spd;    // 話速 	50-300 default:100
		public int vol;    // 音量 	0-300 default:100
		public int pit;    // 高さ 	20-200 default:基本素片に依存
		public int acc;    // アクセント 0-200 default:基本素片に依存
		public int lmd;    // 音程１ 	0-200 default:100
		public int fsc;    // 音程２(サンプリング周波数) 50-200 default:100
		public void Init()
		{
			bas = 0;    // 基本素片 F1E/F2E/M1E (0/1/2)
			spd = 100;    // 話速 	50-300 default:100
			vol = 100;    // 音量 	0-300 default:100
			pit = 100;    // 高さ 	20-200 default:基本素片に依存
			acc = 100;    // アクセント 0-200 default:基本素片に依存
			lmd = 100;    // 音程１ 	0-200 default:100
			fsc = 100;    // 音程２(サンプリング周波数) 50-200 default:100
		}
	}

	[DllImport("AquesTalk")]
	private static extern IntPtr AquesTalk_Synthe(ref AQTK_VOICE pParam, byte[] koe, ref int size);

	[DllImport("AquesTalk")]
	private static extern void AquesTalk_FreeWave(IntPtr wavPtr);


	//https://github.com/Suzeep/audioclip_makerよりbyte配列→再生用float配列への変換処理を拝借
	readonly float RANGE_VALUE_BIT_8 = 1.0f / Mathf.Pow(2, 7);   // 1 / 128
	readonly float RANGE_VALUE_BIT_16 = 1.0f / Mathf.Pow(2, 15); // 1 / 32768
	const int BASE_CONVERT_SAMPLES = 1024 * 20;
	const int BIT_8 = 8;
	const int BIT_16 = 16;

	//---------------------------------------------------------------------------
	// create rawdata( ranged 0.0 - 1.0 ) from binary wav data
	//---------------------------------------------------------------------------
	public float[] CreateRangedRawData(byte[] byte_data, int wav_buf_idx, int samples, int channels, int bit_per_sample)
	{
		float[] ranged_rawdata = new float[samples * channels];

		int step_byte = bit_per_sample / BIT_8;
		int now_idx = wav_buf_idx;

		for (int i = 0; i < (samples * channels); ++i)
		{
			ranged_rawdata[i] = convertByteToFloatData(byte_data, now_idx, bit_per_sample);

			now_idx += step_byte;
		}

		return ranged_rawdata;
	}

	//---------------------------------------------------------------------------
	// convert byte data to float data
	//---------------------------------------------------------------------------
	private float convertByteToFloatData(byte[] byte_data, int idx, int bit_per_sample)
	{
		float float_data = 0.0f;

		switch (bit_per_sample)
		{
			case BIT_8:
				{
					float_data = ((int)byte_data[idx] - 0x80) * RANGE_VALUE_BIT_8;
				}
				break;
			case BIT_16:
				{
					short sample_data = System.BitConverter.ToInt16(byte_data, idx);
					float_data = sample_data * RANGE_VALUE_BIT_16;
				}
				break;
		}

		return float_data;
	}

	public void Synthe()
	{
		Debug.Log("Synthe() start");

		// 不定長情報のメモリ確保
		IntPtr aqtk_p = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(AQTK_VOICE)));
		AQTK_VOICE aqtk_voice = (AQTK_VOICE)Marshal.PtrToStructure(aqtk_p, typeof(AQTK_VOICE));
		aqtk_voice.Init();

		//バイト配列をUTF8の文字コードとしてStringに変換する
		//string koeUtfStr = "ゆっくりしていってね";
		//System.Text.Encoding utf8Enc = System.Text.Encoding.GetEncoding("UTF-8");
		//System.Text.Encoding sjisEnc = System.Text.Encoding.GetEncoding("Shift_JIS");
		//byte[] koeUtfBytes = utf8Enc.GetBytes(koeUtfStr);
		//byte[] koeSjisBytes = System.Text.Encoding.Convert(utf8Enc, sjisEnc, koeUtfBytes);

		//https://helpdesk.unity3d.co.jp/hc/ja/articles/204694010-System-Text-Encoding-%E3%81%A7-Shift-JIS-%E3%82%92%E4%BD%BF%E3%81%84%E3%81%9F%E3%81%84
		//System.Text.Encoding で Shift JIS を使いたい
		//高橋 啓治郎 - 2017年04月07日 17:08
		//Unity の Standalone Player は Shift_JIS (codepage 932) の encoding を含んでおらず、
		//これを System.Text.Encoding で使おうとするとエラーになります。

		//SJIS文字列（あいうえお）
		byte[] koeSjisBytes = { 0x82, 0xa0, 0x82, 0xa2, 0x82, 0xa4, 0x82, 0xa6, 0x82, 0xa8, };

		int size = 0;
		IntPtr wavPtr = AquesTalk_Synthe(ref aqtk_voice, koeSjisBytes, ref size);
		Debug.Log("size : " + size);

		//成功判定
		if (wavPtr == IntPtr.Zero)
		{
			Debug.LogError("ERROR: 音声生成に失敗しました。不正な文字が使われた可能性があります");
		}

		//C#で扱えるようにマネージド側へコピー
		byte[] byte_data = new byte[size];
		Marshal.Copy(wavPtr, byte_data, 0, size);

		//アンマネージドポインタは用が無くなった瞬間に解放
		AquesTalk_FreeWave(wavPtr);

		//float配列に変換
		float[] float_data = CreateRangedRawData(byte_data, 0, size / 2, 1, BIT_16);

		//audioClip作成
		AudioClip audioClip = AudioClip.Create("AquesTalk", float_data.Length, 1, 16000, false);
		audioClip.SetData(float_data, 0);
		audio_.clip = audioClip;

		//再生
		audio_.Play();

		Debug.Log("Synthe() end");
	}

}