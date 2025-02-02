﻿namespace TargaSharp
{
    /// <summary>
    /// Truevision has currently defined seven image types:
    /// <para>0 - No Image Data Included;</para>
    /// <para>1 - Uncompressed, Color-mapped Image;</para>
    /// <para>2 - Uncompressed, True-color Image;</para>
    /// <para>3 - Uncompressed, Black-and-white Image;</para>
    /// <para>9 - Run-length encoded, Color-mapped Image;</para>
    /// <para>10 - Run-length encoded, True-color Image;</para>
    /// <para>11 - Run-length encoded, Black-and-white Image.</para>
    /// Image Data Type codes 0 to 127 are reserved for use by Truevision for general applications.
    /// Image Data Type codes 128 to 255 may be used for developer applications.
    /// </summary>
    public enum ImageType : byte
    {
        NoImageData = 0,
        Uncompressed_ColorMapped = 1,
        Uncompressed_TrueColor,
        Uncompressed_BlackWhite,
        _Truevision_4,
        _Truevision_5,
        _Truevision_6,
        _Truevision_7,
        _Truevision_8,
        RLE_ColorMapped = 9,
        RLE_TrueColor,
        RLE_BlackWhite,
        _Truevision_12,
        _Truevision_13,
        _Truevision_14,
        _Truevision_15,
        _Truevision_16,
        _Truevision_17,
        _Truevision_18,
        _Truevision_19,
        _Truevision_20,
        _Truevision_21,
        _Truevision_22,
        _Truevision_23,
        _Truevision_24,
        _Truevision_25,
        _Truevision_26,
        _Truevision_27,
        _Truevision_28,
        _Truevision_29,
        _Truevision_30,
        _Truevision_31,
        _Truevision_32,
        _Truevision_33,
        _Truevision_34,
        _Truevision_35,
        _Truevision_36,
        _Truevision_37,
        _Truevision_38,
        _Truevision_39,
        _Truevision_40,
        _Truevision_41,
        _Truevision_42,
        _Truevision_43,
        _Truevision_44,
        _Truevision_45,
        _Truevision_46,
        _Truevision_47,
        _Truevision_48,
        _Truevision_49,
        _Truevision_50,
        _Truevision_51,
        _Truevision_52,
        _Truevision_53,
        _Truevision_54,
        _Truevision_55,
        _Truevision_56,
        _Truevision_57,
        _Truevision_58,
        _Truevision_59,
        _Truevision_60,
        _Truevision_61,
        _Truevision_62,
        _Truevision_63,
        _Truevision_64,
        _Truevision_65,
        _Truevision_66,
        _Truevision_67,
        _Truevision_68,
        _Truevision_69,
        _Truevision_70,
        _Truevision_71,
        _Truevision_72,
        _Truevision_73,
        _Truevision_74,
        _Truevision_75,
        _Truevision_76,
        _Truevision_77,
        _Truevision_78,
        _Truevision_79,
        _Truevision_80,
        _Truevision_81,
        _Truevision_82,
        _Truevision_83,
        _Truevision_84,
        _Truevision_85,
        _Truevision_86,
        _Truevision_87,
        _Truevision_88,
        _Truevision_89,
        _Truevision_90,
        _Truevision_91,
        _Truevision_92,
        _Truevision_93,
        _Truevision_94,
        _Truevision_95,
        _Truevision_96,
        _Truevision_97,
        _Truevision_98,
        _Truevision_99,
        _Truevision_100,
        _Truevision_101,
        _Truevision_102,
        _Truevision_103,
        _Truevision_104,
        _Truevision_105,
        _Truevision_106,
        _Truevision_107,
        _Truevision_108,
        _Truevision_109,
        _Truevision_110,
        _Truevision_111,
        _Truevision_112,
        _Truevision_113,
        _Truevision_114,
        _Truevision_115,
        _Truevision_116,
        _Truevision_117,
        _Truevision_118,
        _Truevision_119,
        _Truevision_120,
        _Truevision_121,
        _Truevision_122,
        _Truevision_123,
        _Truevision_124,
        _Truevision_125,
        _Truevision_126,
        _Truevision_127,
        _Other_128,
        _Other_129,
        _Other_130,
        _Other_131,
        _Other_132,
        _Other_133,
        _Other_134,
        _Other_135,
        _Other_136,
        _Other_137,
        _Other_138,
        _Other_139,
        _Other_140,
        _Other_141,
        _Other_142,
        _Other_143,
        _Other_144,
        _Other_145,
        _Other_146,
        _Other_147,
        _Other_148,
        _Other_149,
        _Other_150,
        _Other_151,
        _Other_152,
        _Other_153,
        _Other_154,
        _Other_155,
        _Other_156,
        _Other_157,
        _Other_158,
        _Other_159,
        _Other_160,
        _Other_161,
        _Other_162,
        _Other_163,
        _Other_164,
        _Other_165,
        _Other_166,
        _Other_167,
        _Other_168,
        _Other_169,
        _Other_170,
        _Other_171,
        _Other_172,
        _Other_173,
        _Other_174,
        _Other_175,
        _Other_176,
        _Other_177,
        _Other_178,
        _Other_179,
        _Other_180,
        _Other_181,
        _Other_182,
        _Other_183,
        _Other_184,
        _Other_185,
        _Other_186,
        _Other_187,
        _Other_188,
        _Other_189,
        _Other_190,
        _Other_191,
        _Other_192,
        _Other_193,
        _Other_194,
        _Other_195,
        _Other_196,
        _Other_197,
        _Other_198,
        _Other_199,
        _Other_200,
        _Other_201,
        _Other_202,
        _Other_203,
        _Other_204,
        _Other_205,
        _Other_206,
        _Other_207,
        _Other_208,
        _Other_209,
        _Other_210,
        _Other_211,
        _Other_212,
        _Other_213,
        _Other_214,
        _Other_215,
        _Other_216,
        _Other_217,
        _Other_218,
        _Other_219,
        _Other_220,
        _Other_221,
        _Other_222,
        _Other_223,
        _Other_224,
        _Other_225,
        _Other_226,
        _Other_227,
        _Other_228,
        _Other_229,
        _Other_230,
        _Other_231,
        _Other_232,
        _Other_233,
        _Other_234,
        _Other_235,
        _Other_236,
        _Other_237,
        _Other_238,
        _Other_239,
        _Other_240,
        _Other_241,
        _Other_242,
        _Other_243,
        _Other_244,
        _Other_245,
        _Other_246,
        _Other_247,
        _Other_248,
        _Other_249,
        _Other_250,
        _Other_251,
        _Other_252,
        _Other_253,
        _Other_254,
        _Other_255
    }
}
