﻿using System;
using System.Text;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// This is an ASCII field consisting of 324 bytes which are organized as four lines of 80 characters, each followed by a null terminator.This field is provided, in
    /// addition to the original IMAGE ID field(in the original TGA format), because it was determined that a few developers had used the IMAGE ID field for their own
    /// purposes.This field gives the developer four lines of 80 characters each, to use as an Author Comment area. Each line is fixed to 81 bytes which makes access to the
    /// four lines easy.Each line must be terminated by a null. If you do not use all 80 available characters in the line, place the null after the last character and blank or
    /// null fill the rest of the line.The 81st byte of each of the four lines must be null.
    /// </summary>
    public sealed class AuthorComments : FileField, ICloneable, IEquatable<AuthorComments>
    {
        const int StrNLen = 80; //80 ASCII chars + 1 '\0' = 81 per SrtN!

        /// <summary>
        /// Create a new instance of the <see cref="AuthorComments"/> class.
        /// </summary>
        public AuthorComments() : base(324, 43, 12) { }

        /// <summary>
        /// Create a new instance of the <see cref="AuthorComments"/> class.
        /// </summary>
        public AuthorComments(string str, char blankChar = '\0') : this()
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(str);
#else 
            if(str is null) throw new ArgumentNullException(nameof(str));
#endif
            OriginalString = str;
            BlankSpaceChar = blankChar;
        }

        /// <summary>
        /// Create a new instance of the <see cref="AuthorComments"/> class.
        /// </summary>
        /// <param name="bytes"></param>
        public AuthorComments(byte[] bytes) : base(324, 43, 12, bytes)
        {
            string s = Encoding.ASCII.GetString(bytes, 0, StrNLen);
            s += Encoding.ASCII.GetString(bytes, 81, StrNLen);
            s += Encoding.ASCII.GetString(bytes, 162, StrNLen);
            s += Encoding.ASCII.GetString(bytes, 243, StrNLen);
            switch (s[^1])
            {
                case '\0':
                case ' ':
                    BlankSpaceChar = s[^1];
#if NET8_0_OR_GREATER
                    OriginalString = s.TrimEnd([s[^1]]);
#else
                    OriginalString = s.TrimEnd(new char[] {s[^1]});
#endif
                    break;
                default:
                    OriginalString = s;
                    break;
            }
        }

        public static bool operator ==(AuthorComments item1, AuthorComments item2) => EqualityHelper.Operator(item1, item2);
        public static bool operator !=(AuthorComments item1, AuthorComments item2) => !(item1 == item2);

        /// <summary>
        /// Blank space character to use to fill lines.
        /// </summary>
        public char BlankSpaceChar { get; private set; } = TgaString.DefaultBlankSpaceChar;

        /// <summary>
        /// Original string value.
        /// </summary>
        public string OriginalString { get; private set; } = string.Empty;

        /// <summary>
        /// Make full independent copy of <see cref="AuthorComments"/>.
        /// </summary>
        /// <returns>Copy of <see cref="AuthorComments"/></returns>
        public AuthorComments Clone() => new(OriginalString, BlankSpaceChar);
        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => obj is AuthorComments comments && Equals(comments);
        public bool Equals(AuthorComments? item) => item is not null && OriginalString == item.OriginalString && BlankSpaceChar == item.BlankSpaceChar;

        public override int GetHashCode() => OriginalString.GetHashCode() ^ BlankSpaceChar.GetHashCode();

        /// <summary>
        /// Get ASCII-Like string to first string-terminator, example:
        /// "Some string \0 Some Data \0" - > "Some string".
        /// </summary>
        /// <returns>String to first string-terminator.</returns>
        public string GetString()
        {
            String Str = Encoding.ASCII.GetString(ToBytes());
            for (int i = 1; i < 4; i++)
                Str = Str.Insert((StrNLen + 1) * i + i - 1, "\n");
            return
#if NET8_0_OR_GREATER
                Str.Replace("\0", String.Empty).TrimEnd(['\n']);
#else
                Str.Replace("\0", String.Empty).TrimEnd(new char[] { '\n' });
#endif
        }

        /// <summary>
        /// Convert <see cref="AuthorComments"/> to byte array.
        /// </summary>
        /// <returns>Byte array, every byte is ASCII symbol.</returns>        
        public override byte[] ToBytes()
        {
            char[] characters = new char[81 * 4];
            for (int i = 0; i < characters.Length; i++)
            {
                if ((i + 82) % 81 == 0)
                    characters[i] = TgaString.DefaultEndingChar;
                else
                {
                    int Index = i - i / 81;
                    characters[i] = (Index < OriginalString.Length ? OriginalString[Index] : BlankSpaceChar);
                }
            }
            return Encoding.ASCII.GetBytes(characters);
        }

        /// <summary>
        /// Get ASCII-Like string with string-terminators, example: "Line1 \0\0 Line2 \0\0\0".
        /// </summary>
        /// <returns>String with replaced string-terminators to "\0".</returns>
        public override string? ToString()
        {
            var bytes = ToBytes();
            return bytes is null ? null : Encoding.ASCII.GetString(bytes).Replace("\0", @"\0");
        }


        /// <summary>
        /// How to create this object from a byte array that's been validated.
        /// </summary>
        /// <param name="bytes"></param>
        protected override void FromBytes(byte[] bytes)
        {
            string s = Encoding.ASCII.GetString(bytes, 0, StrNLen);
            s += Encoding.ASCII.GetString(bytes, 81, StrNLen);
            s += Encoding.ASCII.GetString(bytes, 162, StrNLen);
            s += Encoding.ASCII.GetString(bytes, 243, StrNLen);

            switch (s[^1])
            {
                case '\0':
                case ' ':
                    BlankSpaceChar = s[^1];
                    OriginalString = s.TrimEnd(
#if NET8_0_OR_GREATER
                        [ s[^1] ]
#else
                        new char[] { s[^1] }
#endif
                    );
                    break;
                default:
                    OriginalString = s;
                    break;
            }
        }
    }
}
