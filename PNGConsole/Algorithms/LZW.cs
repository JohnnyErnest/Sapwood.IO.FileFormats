using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sapwood.IO.FileFormats.Algorithms
{
    public class LZW
    {
        int BITS;
        int HASHING_SHIFT;
        int MAX_VALUE;
        int MAX_CODE;
        int TABLE_SIZE;

        //#define BITS 12                   /* Setting the number of bits to 12, 13*/
        //#define HASHING_SHIFT (BITS-8)    /* or 14 affects several constants.    */
        //#define MAX_VALUE (1 << BITS) - 1 /* Note that MS-DOS machines need to   */
        //#define MAX_CODE MAX_VALUE - 1    /* compile their code in large model if*/
        //        /* 14 bits are selected.               */
        //#if BITS == 14
        //#define TABLE_SIZE 18041        /* The string table size needs to be a */
        //#endif                            /* prime number that is somewhat larger*/
        //#if BITS == 13                    /* than 2**BITS.                       */
        //#define TABLE_SIZE 9029
        //#endif
        //#if BITS <= 12
        //#define TABLE_SIZE 5021
        //#endif

        //void* malloc();

        int[] code_value;                  /* This is the code value array        */
        uint[] prefix_code;        /* This array holds the prefix codes   */
        byte[] append_character;  /* This array holds the appended chars */
        byte[] decode_stack; /* This array holds the decoded string */
        //int decode_stack_ptr = 0;
        ulong bit_buffer;
        int bit_counter;

        /*
        ** This is the compression routine.  The code should be a fairly close
        ** match to the algorithm accompanying the article.
        **
        */

        public void InitTable(int bits)
        {
            BITS = bits;
            HASHING_SHIFT = BITS - 8;
            MAX_VALUE = (1 << BITS) - 1;
            MAX_CODE = MAX_VALUE - 1;
            TABLE_SIZE = 5021;

            code_value = new int[TABLE_SIZE];                  /* This is the code value array        */
            prefix_code = new uint[TABLE_SIZE];        /* This array holds the prefix codes   */
            append_character = new byte[TABLE_SIZE];  /* This array holds the appended chars */
            decode_stack = new byte[TABLE_SIZE]; /* This array holds the decoded string */
        }

        public LZW(int bits)
        {
            InitTable(bits);
        }

        public LZW()
        {
            InitTable(12);

            //byte[] textBytes = File.ReadAllBytes(@$"c:\users\johnr\Documents\4.c");
            //MemoryStream input = new MemoryStream(textBytes);
            //MemoryStream output = new MemoryStream();
            //compress(input, output);
            //output.Position = 0;
            //byte[] compressedBytes = new byte[output.Length];
            //byte[] cProgramCompare = File.ReadAllBytes($@"C:\Users\johnr\source\repos\CProject\CProject\test.lzw");
            //output.Read(compressedBytes, 0, compressedBytes.Length);
            //for (int z = 0; z < compressedBytes.Length; z++)
            //{
            //    if (compressedBytes[z] != cProgramCompare[z])
            //    {
            //        Console.WriteLine($"Invalid Compression {z} - {compressedBytes[z]} - {cProgramCompare[z]}");
            //    }
            //}
            //input = new MemoryStream(compressedBytes);
            //output = new MemoryStream();
            //expand(input, output);
            //output.Position = 0;
            //byte[] finalBytes = new byte[output.Length];
            //output.Read(finalBytes, 0, finalBytes.Length);
            //string finalStr = Encoding.Default.GetString(finalBytes);
            //byte[] testOutputBytes = File.ReadAllBytes($@"C:\Users\johnr\source\repos\CProject\CProject\test.out");
            //for (int z = 0; z < finalBytes.Length; z++)
            //{
            //    if (finalBytes[z] != testOutputBytes[z])
            //    {
            //        Console.WriteLine($"Invalid Compression {z} - {compressedBytes[z]} - {cProgramCompare[z]}");
            //    }
            //}
        }

        public void Compress(Stream input, Stream output)
        {
            uint next_code;
            uint character;
            uint string_code;
            uint index;
            int i;

            bit_counter = 0;
            bit_buffer = 0;

            next_code = 256;              /* Next code is the next available string code*/
            for (i = 0; i < TABLE_SIZE; i++)  /* Clear out the string table before starting */
                code_value[i] = -1;

            i = 0;
            //Console.Write("Compressing...\n");
            string_code = (uint)input.ReadByte();    /* Get the first code                         */
            /*
            ** This is the main loop where it all happens.  This loop runs util all of
            ** the input has been exhausted.  Note that it stops adding codes to the
            ** table after all of the possible codes have been defined.
            */
            bool eof = false;
            while (!eof)
            {
                int charTest = input.ReadByte();
                if (charTest == -1)
                    eof = true;
                if (!eof)
                {
                    character = (uint)charTest;

                    index = (uint)find_match((int)string_code, (uint)character);/* See if the string is in */
                    if (code_value[index] != -1)            /* the table.  If it is,   */
                        string_code = (uint)code_value[index];        /* get the code value.  If */
                    else                                    /* the string is not in the*/
                    {                                       /* table, try to add it.   */
                        if (next_code <= MAX_CODE)
                        {
                            code_value[index] = (int)next_code++;
                            prefix_code[index] = string_code;
                            append_character[index] = (byte)character;
                        }
                        output_code(output, string_code);  /* When a string is found  */
                        string_code = character;            /* that is not in the table*/
                    }                                   /* I output the last string*/
                }
            }                                     /* after adding the new one*/
            /*
            ** End of the main loop.
            */
            output_code(output, string_code); /* Output the last code               */
            output_code(output, (uint)MAX_VALUE);   /* Output the end of buffer code      */
            output_code(output, 0);           /* This code flushes the output buffer*/
            //Console.Write("\n");
            output.Flush();
        }

        /*
        ** This is the hashing routine.  It tries to find a match for the prefix+char
        ** string in the string table.  If it finds it, the index is returned.  If
        ** the string is not found, the first available index in the string table is
        ** returned instead.
        */
        int find_match(int hash_prefix, uint hash_character)
        {
            int index;
            int offset;

            //index = (hash_character << HASHING_SHIFT) ^ hash_prefix;
            index = (int)((hash_character << HASHING_SHIFT) ^ hash_prefix);
            if (index == 0)
                offset = 1;
            else
                offset = TABLE_SIZE - index;
            while (true)
            {
                if (code_value[index] == -1)
                    return (index);
                if (prefix_code[index] == hash_prefix && append_character[index] == hash_character)
                    return (index);
                index -= offset;
                if (index < 0)
                    index += TABLE_SIZE;
            }
        }

        ///*
        //**  This is the expansion routine.  It takes an LZW format file, and expands
        //**  it to an output file.  The code here should be a fairly close match to
        //**  the algorithm in the accompanying article.
        //*/

        public void Decompress(Stream input, Stream output)
        {
            uint next_code;
            uint new_code;
            uint old_code;
            int character;
            //int counter;
            decode_stack = new byte[TABLE_SIZE];
            code_value = new int[TABLE_SIZE];                  /* This is the code value array        */
            prefix_code = new uint[TABLE_SIZE];        /* This array holds the prefix codes   */
            append_character = new byte[TABLE_SIZE];  /* This array holds the appended chars */
            int decode_stack_pointer = 0;
            //int initial_decode_stack_pointer = 0;
            int string_pointer;
            //*str = actual character
            //str = pointer

            bit_counter = 0;
            bit_buffer = 0;

            next_code = 256;           /* This is the next available code to define */
            //counter = 0;               /* Counter is used as a pacifier.            */
            //Console.Write("Expanding...\n");

            InputCodeResult input_code_result = input_code(input);
            if (input_code_result.IsEOF)
            {
                return;
                //throw new ArgumentException("Input Stream is Empty");
            }
            old_code = input_code_result.Code;  /* Read in the first code, initialize the */
            character = (int)old_code;

            // Check to see if a byte or a DWORD is written.
            output.WriteByte((byte)old_code);

            while(input_code_result.IsEOF == false)
            {
                input_code_result = input_code(input);
                if (!input_code_result.IsEOF)
                {
                    new_code = input_code_result.Code;

                    if (new_code >= next_code)
                    {
                        decode_stack[decode_stack_pointer] = (byte)character;
                        string_pointer = decode_string(decode_stack_pointer + 1, old_code);
                    }
                    else
                    {
                        string_pointer = decode_string(decode_stack_pointer, new_code);
                        int c = decode_stack_pointer;
                    }

                    character = decode_stack[string_pointer];
                    while (string_pointer >= decode_stack_pointer)
                    {
                        //string_pointer--;
                        byte outc = decode_stack[string_pointer--];
                        output.WriteByte(outc);
                    }

                    if (next_code <= MAX_CODE)
                    {
                        prefix_code[next_code] = old_code;
                        append_character[next_code] = (byte)character;
                        next_code++;
                    }
                    old_code = new_code;
                }
            }
            output.Flush();
        }


        /*
        ** This routine simply decodes a string from the string table, storing
        ** it in a buffer.  The buffer can then be output in reverse order by
        ** the expansion program.
        */
        int decode_string(int stack_pointer, uint code)
        {
            int i;

            i = 0;
            while (code > 255)
            {
                decode_stack[stack_pointer] = append_character[code];
                stack_pointer++;
                code = prefix_code[code];
                if (i++ >= MAX_CODE)
                {
                    Console.Write("Fatal error during code expansion.\n");
                    //exit(-3);
                }
            }
            decode_stack[stack_pointer] = (byte)code;
            return (stack_pointer);
        }

        class InputCodeResult
        {
            public uint Code { get; set; }
            public bool IsEOF { get; set; }
        }

        InputCodeResult input_code(Stream input)
        {
            uint return_value;
            while (bit_counter <= 24)
            {
                bool isEOF = false;
                int inputByte = input.ReadByte();
                if (inputByte == -1)
                {
                    isEOF = true;
                }
                if (input.Position == input.Length)
                {
                    isEOF = true;
                }
                if (isEOF)
                    return new InputCodeResult() { IsEOF = true };
                bit_buffer |= ((ulong)inputByte << (24 - bit_counter));
                bit_counter += 8;
            }
            ulong ret1 = bit_buffer;
            ret1 >>= (32 - BITS);
            return_value = (uint)ret1;
            bit_buffer <<= BITS;
            bit_buffer = (uint)bit_buffer;
            bit_counter -= BITS;
            //if (return_value == 4095)
            //{
            //    if (BITS < 12) BITS++;
            //    InitTable(BITS);
            //}
            //else if (return_value == 4096)
            //{
            //    return new InputCodeResult() { IsEOF = true };
            //}
            return (new InputCodeResult() { IsEOF = false, Code = return_value });
        }

        void output_code(Stream output, uint code)
        {
            bit_buffer |= (ulong) code << (32 - BITS - bit_counter);
            bit_counter += BITS;

            while (bit_counter >= 8)
            {
                byte temp = (byte)((bit_buffer >> 24) & 255);                
                output.WriteByte(temp);
                //pWriter.WriteByte((byte)((output_bit_buffer >> 24) & 255)); //write byte from bit buffer
                //_iBitBuffer <<= 8; //remove written byte from buffer
                //_iBitCounter -= 8; //decrement counter

                //putc((uint)(output_bit_buffer >> 24), output);
                bit_buffer <<= 8;
                bit_counter -= 8;
            }
        }
    }
}

//# include <stdio.h>
//# include <stdlib.h>
//# include <string.h>


///*
// * Forward declarations
// */
//void compress(FILE* input, FILE* output);
//void expand(FILE* input, FILE* output);
//int find_match(int hash_prefix, unsigned int hash_character);
//void output_code(FILE* output, unsigned int code);
//unsigned int input_code(FILE* input);
//unsigned char* decode_string(unsigned char* buffer, unsigned int code);

///********************************************************************
//**
//** This program gets a file name from the command line.  It compresses the
//** file, placing its output in a file named test.lzw.  It then expands
//** test.lzw into test.out.  Test.out should then be an exact duplicate of
//** the input file.
//**
//*************************************************************************/

//main(int argc, char* argv[])
//{
//    FILE* input_file;
//    FILE* output_file;
//    FILE* lzw_file;
//    char input_file_name[81];

//    /*
//    **  The three buffers are needed for the compression phase.
//    */
//    code_value = (int*)malloc(TABLE_SIZE * sizeof(int));
//    prefix_code = (unsigned int*)malloc(TABLE_SIZE * sizeof(unsigned int));
//    append_character = (unsigned char*)malloc(TABLE_SIZE * sizeof(unsigned char));
//    if (code_value == NULL || prefix_code == NULL || append_character == NULL)
//    {
//        printf("Fatal error allocating table space!\n");
//        exit(-1);
//    }
//    /*
//    ** Get the file name, open it up, and open up the lzw output file.
//    */
//    if (argc > 1)
//        strcpy(input_file_name, argv[1]);
//    else
//    {
//        printf("Input file name? ");
//        scanf("%s", input_file_name);
//    }
//    input_file = fopen(input_file_name, "rb");
//    lzw_file = fopen("test.lzw", "wb");
//    if (input_file == NULL || lzw_file == NULL)
//    {
//        printf("Fatal error opening files.\n");
//        exit(-1);
//    };
//    /*
//    ** Compress the file.
//    */
//    compress(input_file, lzw_file);
//    fclose(input_file);
//    fclose(lzw_file);
//    free(code_value);
//    /*
//    ** Now open the files for the expansion.
//    */
//    lzw_file = fopen("test.lzw", "rb");
//    output_file = fopen("test.out", "wb");
//    if (lzw_file == NULL || output_file == NULL)
//    {
//        printf("Fatal error opening files.\n");
//        exit(-2);
//    };
//    /*
//    ** Expand the file.
//    */
//    expand(lzw_file, output_file);
//    fclose(lzw_file);
//    fclose(output_file);

//    free(prefix_code);
//    free(append_character);
//}