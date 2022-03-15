using System;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace lB2Aidana
{


    class Program
    {

        // IMPORT START

        // function CreateFile
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
        string fileName,
        [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
        IntPtr securityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        FileAttributes flags,
        IntPtr template
        );

        [Flags]
        public enum FileMapProtection : uint
        {
            PageReadonly = 0x02,
            PageReadWrite = 0x04,
            PageWriteCopy = 0x08,
            PageExecuteRead = 0x20,
            PageExecuteReadWrite = 0x40,
            SectionCommit = 0x8000000,
            SectionImage = 0x1000000,
            SectionNoCache = 0x10000000,
            SectionReserve = 0x4000000,
        }

        // function CreateFileMapping
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]  //отображение в памяти в виртуальную память
        public static extern SafeFileHandle CreateFileMapping(
        SafeFileHandle hFile,           //дескриптор файла
        IntPtr lpFileMappingAttributes, //право на наследование
        FileMapProtection flProtect,    //защита
        uint dwMaximumSizeHigh,         //макс размер
        uint dwMaximumSizeLow,          //мин размер
        [MarshalAs(UnmanagedType.LPStr)] string lpName
        );

        // function GetFileSizeEx
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetFileSizeEx(SafeFileHandle hFile, out uint lpFileSize);

        // function MapViewOfFile
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern ulong MapViewOfFile(
        SafeFileHandle hFileMappingObject,
        FileMapAccess dwDesiredAccess,  //разр на чтение\запись
        uint UInt32dwFileOffsetHigh,       //макс размер
        uint UInt32dwFileOffsetLow,         //мин размер
        UIntPtr dwNumberOfBytesToMap        //Число байтов сопоставления файла,
        );

        [Flags]
        public enum FileMapAccess : uint
        {
            FileMapCopy = 0x0001,
            FileMapWrite = 0x0002,
            FileMapRead = 0x0004,
            FileMapAllAccess = 0x001f,
            FileMapExecute = 0x0020,
        }

        // function CopyMemory
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(byte[] dest, ulong tt, uint count);

        // function CloseHandle
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(SafeFileHandle handle);

        // IMPORT END

        static void Main(string[] args)
        {
            /*
                Задание №1
                Создать текстовый файл 
            */

            String pathFile = "tt.txt";

            /* 
                Задани №2
                Создасть объект File на базе созданного в предыдущем пункте файла,
                используя АРI-функцию CreateFile. Вывести значение дескриптора объекта File.
            */

            byte[] buf = new byte[ushort.MaxValue]; // buffer для чтения впервые 65535 символов из  исходного файла

            SafeFileHandle file = CreateFile(
                fileName: pathFile,
                fileAccess: FileAccess.ReadWrite, // для чтения + для записи 
                fileShare: FileShare.Write | FileShare.Read | FileShare.Delete,
                securityAttributes: IntPtr.Zero, // атрибуты по умолчанию
                creationDisposition: FileMode.OpenOrCreate, // если файл существует открыть иначе создать
                flags: FileAttributes.Normal, // атрибуты по умолчанию
                template: IntPtr.Zero        // атрибуты по умолчанию
            );

            // чтение данных по дескриптору файла
            FileStream diskStreamToReadWrite = new FileStream(file, FileAccess.ReadWrite);
            diskStreamToReadWrite.Read(buf, 0, ushort.MaxValue);



            // Вывод значение дескриптора файла
            Console.WriteLine("Значение дескриптора файла = " + file.DangerousGetHandle());

            /* 
                Задания №3
                Используя дескриптор объекта File-mapping, а также API-функцию
                MapViewOfFile отобразить части файла в память. Данная функция назначает область
                виртуальной памяти, выделяемой этому файлу. Базовый адрес выделенной области
                памяти является дескриптором представления этой области в виде отображения файла
            */

            // создание FileMapping с помощью с помощью дескриптора файла file

            SafeFileHandle fileMapping = CreateFileMapping(
                hFile: file, //дескриптор
                lpFileMappingAttributes: IntPtr.Zero, //право на наследование
                flProtect: FileMapProtection.PageReadonly, //защита файлов 
                dwMaximumSizeHigh: 0, // макс размер 
                dwMaximumSizeLow: 0, //мин размер
                "fileMapping"
            );

            Console.WriteLine("Значение дескриптора файлового отображения  = " + fileMapping.DangerousGetHandle());

            // Получение размера исходного файла
            uint fileSizeInByte = 0;
            GetFileSizeEx(file, out fileSizeInByte);

            // отображение части файла в память + получение начального адреса отображенного областа памяти
            ulong mapViewFile = MapViewOfFile(fileMapping, FileMapAccess.FileMapRead, 0, 0, UIntPtr.Zero);

            Console.WriteLine("начальный адрес памяти для файла = " + mapViewFile);
            Console.WriteLine("размер файла = " + fileSizeInByte + " байт");

            // прочитать содержимое исходного файла записанный в блок памяти
            byte[] fileFromMemory = new byte[fileSizeInByte];

            // копировать на блок памяти myFileMemory(содержимое tt.text) начиная с (mapViewFile) до (mapViewFile + fileSizeInByte)
            CopyMemory(fileFromMemory, mapViewFile, fileSizeInByte);

            /*
                Заданиа №4
                Используя базовый адрес и функцию CopyMemory прочитайте информацию из отображаемого файла.
                Согласно варианту измените текстовый файл и запишите информацию в этот же файл:
            */

            // текст из файла помещенный в область памяти
            string textFromMemory = System.Text.Encoding.UTF8.GetString(fileFromMemory);
            Console.WriteLine("\nтекст прочитанной из файла помещенный в область памяти: \n\n" + textFromMemory);

            // Вариант №14. Заменить все буквы на коды ASCII 


            string text = textFromMemory;
            int count = 0;
            byte[] textToWrite = Encoding.ASCII.GetBytes(text);
            foreach (byte b in textToWrite)
                Console.Write(b + " ");

       
            diskStreamToReadWrite.Write(textToWrite, 0, textToWrite.Length);


            try { diskStreamToReadWrite.Close(); } catch { } // закрытие потока tt.text
            try { file.Close(); } catch { } // закрытие дескриптора

            System.Console.ReadKey(true);


        }
    }
}
