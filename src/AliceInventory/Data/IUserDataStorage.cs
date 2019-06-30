using System;

namespace AliceInventory.Data
{
    public interface IUserDataStorage
    {
        void AddEntry(string userId, string entryName, double count, Data.UnitOfMeasure unit);
        void DeleteEntry(string userId, string entryName, double count, Data.UnitOfMeasure unit);
        Data.Entry[] ReadAllEntries(string userId);
        bool ClearInventory(string userId);
        string GetUserEmail(string userId);
        bool SetUserEmail(string userId, string email);
        string DeleteUserEmail(string userId);
    }
}
