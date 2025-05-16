using System.Windows;
using System.Runtime.CompilerServices; // <-- Добавьте эту строку

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,    //where theme specific resource dictionaries are located
                                        //(used if a resource is not found in the page,
                                        // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]

// Добавьте эту строку ниже using директив и выше других атрибутов
// Замените "YourTestAssemblyName" на точное имя вашей тестовой сборки
// Например: "SharpPixilizer"
[assembly: InternalsVisibleTo("SharpPixilizerTests")]