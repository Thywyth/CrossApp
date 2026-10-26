namespace Core.Domain;

// Додаткове завдання 3: явний стан життєвого циклу товару.
// Допустимі переходи перевіряє Product.ChangeState через switch expression:
//   Active    -> Suspended, Discontinued
//   Suspended -> Active, Discontinued
//   Discontinued — кінцевий стан, з нього переходів немає
public enum ProductState
{
    Active,
    Suspended,
    Discontinued
}
