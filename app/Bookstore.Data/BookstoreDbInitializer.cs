using Bookstore.Domain.Books;
using Bookstore.Domain.ReferenceData;
using System.Collections.Generic;
using System.Linq;

namespace Bookstore.Data
{
    /// <summary>
    /// Seeds initial reference and book data if the database is empty.
    /// Called at application startup via DatabaseSeeder.SeedAsync().
    /// </summary>
    public static class BookstoreDbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.ReferenceData.Any()) return;

            var referenceDataItems = new List<ReferenceDataItem>
            {
                new ReferenceDataItem(ReferenceDataType.BookType, "Hardcover"),
                new ReferenceDataItem(ReferenceDataType.BookType, "Trade Paperback"),
                new ReferenceDataItem(ReferenceDataType.BookType, "Mass Market Paperback"),

                new ReferenceDataItem(ReferenceDataType.Condition, "New"),
                new ReferenceDataItem(ReferenceDataType.Condition, "Like New"),
                new ReferenceDataItem(ReferenceDataType.Condition, "Good"),
                new ReferenceDataItem(ReferenceDataType.Condition, "Acceptable"),

                new ReferenceDataItem(ReferenceDataType.Genre, "Biographies"),
                new ReferenceDataItem(ReferenceDataType.Genre, "Children's Books"),
                new ReferenceDataItem(ReferenceDataType.Genre, "History"),
                new ReferenceDataItem(ReferenceDataType.Genre, "Literature & Fiction"),
                new ReferenceDataItem(ReferenceDataType.Genre, "Mystery, Thriller & Suspense"),
                new ReferenceDataItem(ReferenceDataType.Genre, "Science Fiction & Fantasy"),
                new ReferenceDataItem(ReferenceDataType.Genre, "Travel"),

                new ReferenceDataItem(ReferenceDataType.Publisher, "Arcadia Books"),
                new ReferenceDataItem(ReferenceDataType.Publisher, "Astral Publishing"),
                new ReferenceDataItem(ReferenceDataType.Publisher, "Moonlight Publishing"),
                new ReferenceDataItem(ReferenceDataType.Publisher, "Dreamscape Press"),
                new ReferenceDataItem(ReferenceDataType.Publisher, "Enchanted Library"),
                new ReferenceDataItem(ReferenceDataType.Publisher, "Fantasia House"),
                new ReferenceDataItem(ReferenceDataType.Publisher, "Horizon Books"),
                new ReferenceDataItem(ReferenceDataType.Publisher, "Infinity Press"),
                new ReferenceDataItem(ReferenceDataType.Publisher, "Paradigm Publishing"),
                new ReferenceDataItem(ReferenceDataType.Publisher, "Aurora Publishing"),
            };

            context.ReferenceData.AddRange(referenceDataItems);
            context.SaveChanges();

            if (context.Book.Any()) return;

            // Retrieve inserted reference data items by type and order to get their IDs
            var publishers = context.ReferenceData
                .Where(x => x.DataType == ReferenceDataType.Publisher)
                .OrderBy(x => x.Id)
                .ToList();

            var bookTypes = context.ReferenceData
                .Where(x => x.DataType == ReferenceDataType.BookType)
                .OrderBy(x => x.Id)
                .ToList();

            var genres = context.ReferenceData
                .Where(x => x.DataType == ReferenceDataType.Genre)
                .OrderBy(x => x.Id)
                .ToList();

            var conditions = context.ReferenceData
                .Where(x => x.DataType == ReferenceDataType.Condition)
                .OrderBy(x => x.Id)
                .ToList();

            // publishers[0]=Arcadia(15), [1]=Astral(16), [2]=Moonlight(17), [3]=Dreamscape(18),
            // [4]=Enchanted(19), [5]=Fantasia(20), [6]=Horizon(21), [7]=Infinity(22),
            // [8]=Paradigm(23), [9]=Aurora(24)
            // bookTypes[0]=Hardcover(1), [1]=Trade Paperback(2), [2]=Mass Market Paperback(3)
            // conditions[0]=New(4), [1]=Like New(5), [2]=Good(6), [3]=Acceptable(7)
            // genres[0]=Biographies(8), [1]=Children's(9), [2]=History(10), [3]=Lit&Fiction(11),
            // [4]=Mystery(12), [5]=SciFi(13), [6]=Travel(14)

            var books = new List<Book>
            {
                new Book("2020: The Apocalypse", "Li Juan", "6556784356",
                    publishers[0].Id, bookTypes[0].Id, genres[5].Id, conditions[1].Id,
                    10.95M, 25, null, null, "/Content/Images/coverimages/apocalypse.png"),

                new Book("Children Of Iron", "Nikki Wolf", "7665438976",
                    publishers[1].Id, bookTypes[0].Id, genres[3].Id, conditions[2].Id,
                    13.95M, 3, null, null, "/Content/Images/coverimages/childrenofiron.png"),

                new Book("Gold In The Dark", "Richard Roe", "5442280765",
                    publishers[2].Id, bookTypes[0].Id, genres[5].Id, conditions[1].Id,
                    6.50M, 10, null, null, "/Content/Images/coverimages/goldinthedark.png"),

                new Book("Leagues Of Smoke", "Pat Candella", "4556789542",
                    publishers[3].Id, bookTypes[1].Id, genres[3].Id, conditions[3].Id,
                    3M, 1, null, null, "/Content/Images/coverimages/leaguesofsmoke.png"),

                new Book("Alone With The Stars", "Carlos Salazar", "4563358087",
                    publishers[4].Id, bookTypes[1].Id, genres[4].Id, conditions[1].Id,
                    15.95M, 5, null, null, "/Content/Images/coverimages/alonewiththestars.png"),

                new Book("The Girl In The Polaroid", "Terri Whitlock", "2354435678",
                    publishers[5].Id, bookTypes[0].Id, genres[4].Id, conditions[2].Id,
                    8.25M, 2, null, null, "/Content/Images/coverimages/girlinthepolaroid.png"),

                new Book("1001 Jokes", "Mary Major", "6554789632",
                    publishers[6].Id, bookTypes[1].Id, genres[3].Id, conditions[1].Id,
                    13.95M, 7, null, null, "/Content/Images/coverimages/1001jokes.png"),

                new Book("My Search For Meaning", "Mateo Jackson", "4558786554",
                    publishers[7].Id, bookTypes[2].Id, genres[0].Id, conditions[3].Id,
                    5M, 15, null, null, "/Content/Images/coverimages/mysearchformeaning.png"),
            };

            context.Book.AddRange(books);
            context.SaveChanges();
        }
    }
}
