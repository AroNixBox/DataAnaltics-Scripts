namespace MySQL {
    // Used to store the User Data
    [System.Serializable]
    public class DatabaseUser {
        // CASE SENSITIVE: Need to be the same Type as in the SQL Database!
        public string Username;
        public string Email;
        public string Password;
    }
}