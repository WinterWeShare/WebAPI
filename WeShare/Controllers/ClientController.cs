using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.EntityFramework;
using WebAPI.Models.Invoice;
using WebAPI.Models.Security;

namespace WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class ClientController : ControllerBase
{
    private DbWeshareContext _context = new();

    /// <summary>
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    private int GetUserToGroupId(int userId, int groupId)
    {
        return (from utg in _context.UserToGroups
            where utg.UserId == userId && utg.GroupId == groupId
            select utg.Id).FirstOrDefault();
    }

    /// <summary>
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    private IEnumerable<int> GetUserToGroupIdsByGroupId(int groupId)
    {
        return from utg in _context.UserToGroups
            where utg.GroupId == groupId
            select utg.Id;
    }

    /// <summary>
    ///     Sends a mail to the user containing the session id.
    ///     Saves the session id in the database.
    /// </summary>
    /// <param name="email"></param>
    [HttpPost]
    [Route(nameof(CreateSession) + "/{email}")]
    public void CreateSession(string email)
    {
        var user = (from a in _context.Users
            where a.Email == email
            select a).FirstOrDefault();
        if (user is null) throw new Exception("User not found");

        if (_context.UserSessions.Any(a => a.UserId == user.Id && a.Date.Date == DateTime.Now.Date))
        {
            _context.UserSessions.RemoveRange(_context.UserSessions.Where(a => a.UserId == user.Id && a.Date.Date == DateTime.Now.Date));
            _context.SaveChanges();
        }

        // Create the TwoFactor code
        TwoFactor twoFactor = new(user.Email);
        // Send it to the user
        twoFactor.SendCode();
        // Encrypt the code
        Encryption.Create(twoFactor.Code.ToString(), out var sessionKey, out var salt);
        // Save the encrypted code and salt
        _context.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            SessionKey = sessionKey,
            Salt = salt,
            Date = DateTime.Now
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Checks if the user has the correct session key.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     A boolean indicating if the user has the correct session key.
    /// </returns>
    [HttpGet]
    [Route(nameof(ValidateSessionKey) + "/{sessionKey}/{userId}")]
    public IEnumerable<bool> ValidateSessionKey(int sessionKey, int userId)
    {
        // Get the user session
        var userSession = (from a in _context.UserSessions
            where a.UserId == userId
            select a).FirstOrDefault() ?? new UserSession
        {
            Date = DateTime.Now,
            SessionKey = string.Empty,
            Salt = string.Empty
        };

        // If the session is from yesterday, delete it and throw an exception
        if (userSession.Date.Date < DateTime.Now.Date)
        {
            _context.UserSessions.Remove(userSession);
            _context.SaveChanges();
        }

        var success = Encryption.Compare(sessionKey.ToString(), userSession.SessionKey, userSession.Salt);
        Console.WriteLine($"User {userId} has validated session key {sessionKey} with result {success}");
        yield return success;
    }

    /// <summary>
    ///     Gets an id of an user by their email
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="email"></param>
    /// <returns>
    ///     The id of the user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserId) + "/{sessionKey}/{email}")]
    public IEnumerable<int> GetUserId(int sessionKey, string email)
    {
        var userId = (from u in _context.Users
            where u.Email == email
            select u.Id).FirstOrDefault();
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        return from u in _context.Users
            where u.Email == email
            select u.Id;
    }

    /// <summary>
    ///     Checks if the user exists.
    /// </summary>
    /// <param name="email" />
    /// <returns>
    ///     A bool value representing if the user exists.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsUserExists) + "/{email}")]
    public IEnumerable<bool> IsUserExists(string email)
    {
        yield return (from u in _context.Users
            where u.Email == email
            select u).FirstOrDefault() is not null;
    }

    /// <summary>
    ///     Gets if the current user is the owner of a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId" />
    /// <returns>
    ///     A bool value representing if the user is the owner of the group.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsOwner) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<bool> IsOwner(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        yield return (from g in _context.Groups
            join utg in _context.UserToGroups on g.Id equals utg.GroupId
            where g.Id == groupId && utg.UserId == userId && utg.IsOwner
            select g).FirstOrDefault() is not null;
    }
    
    /// <summary>
    ///     Gets if a user is the owner of a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId" />
    /// <param name="targetId"></param>
    /// <returns>
    ///     A bool value representing if the user is the owner of the group.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsUserOwner) + "/{sessionKey}/{userId}/{groupId}/{targetId}")]
    public IEnumerable<bool> IsUserOwner(int sessionKey, int userId, int groupId, int targetId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        yield return (from g in _context.Groups
            join utg in _context.UserToGroups on g.Id equals utg.GroupId
            where g.Id == groupId && utg.UserId == targetId && utg.IsOwner
            select g).FirstOrDefault() is not null;
    }

    /// <summary>
    ///     Checks if the user is deactivated.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId" />
    /// <returns>
    ///     A bool value representing if the user is deactivated
    /// </returns>
    [HttpGet]
    [Route(nameof(IsUserDeactivatedThemselves) + "/{sessionKey}/{userId}")]
    public IEnumerable<bool> IsUserDeactivatedThemselves(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
    
        yield return (from du in _context.DeactivatedUsers
            where du.Id == userId && du.ByAdmin == false
            select du).FirstOrDefault() is not null;
    }

    /// <summary>
    ///     Checks if a user is deactivated by an admin.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId" />
    /// <returns>
    ///     A bool value representing if the user is deactivated by an admin.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsUserDeactivatedByAdmin) + "/{sessionKey}/{userId}")]
    public IEnumerable<bool> IsUserDeactivatedByAdmin(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        yield return (from du in _context.DeactivatedUsers
            where du.Id == userId && du.ByAdmin
            select du).FirstOrDefault() is not null;
    }

    /// <summary>
    ///     Checks if the user is in a group.
    /// </summary>
    /// <param name="sessionKey" />
    /// <param name="userId" />
    /// <param name="groupId" />
    /// <returns>
    ///     A bool value representing if the user is in the group.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsInGroup) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<bool> IsInGroup(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        yield return (from utg in _context.UserToGroups
            where utg.UserId == userId && utg.GroupId == groupId
            select utg).FirstOrDefault() is not null;
    }

    /// <summary>
    ///     Checks if a group is closed.
    /// </summary>
    /// <param name="sessionKey" />
    /// <param name="userId" />
    /// <param name="groupId" />
    /// <returns>
    ///     A bool value representing if the group is closed.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsGroupClosed) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<bool> IsGroupClosed(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        yield return (from g in _context.Groups
            where g.Id == groupId && g.Closed
            select g).FirstOrDefault() is not null;
    }

    /// <summary>
    ///     Gets a user from the database.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     A User object.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUser) + "/{sessionKey}/{userId}")]
    public IEnumerable<User> GetUser(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        return from user in _context.Users
            where user.Id == userId
            select user;
    }

    /// <summary>
    ///     Gets all the users in a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A list of users.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUsers) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<User> GetUsers(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        return from u in _context.Users
            join utg in _context.UserToGroups
                on u.Id equals utg.UserId
            where utg.GroupId == groupId
            select u;
    }

    /// <summary>
    ///     Gets the user's name by their userToGroup id.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="userToGroupId"></param>
    /// <returns>
    ///     A string representing the user's name.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserNameByUserToGroupId) + "/{sessionKey}/{userId}/{userToGroupId}")]
    public IEnumerable<string> GetUserNameByUserToGroupId(int sessionKey, int userId, int userToGroupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        return from u in _context.Users
            join utg in _context.UserToGroups
                on u.Id equals utg.UserId
            where utg.Id == userToGroupId
            select $"{u.FirstName} {u.LastName}";
    }

    /// <summary>
    ///     Inserts a new user in the database.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="phoneNumber"></param>
    [HttpPost]
    [Route(nameof(InsertUser) + "/{email}/{firstName}/{lastName}/{phoneNumber}")]
    public void InsertUser(string email, string firstName, string lastName, string phoneNumber)
    {
        var user = (from u in _context.Users
            where u.Email == email || u.PhoneNumber == phoneNumber
            select u).FirstOrDefault();
        if (user is not null)
            throw new Exception($"User with email {email} or phone number {phoneNumber} already exists.");

        _context.Users.Add(new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber
        });
        _context.SaveChanges();
        
        user = (from u in _context.Users
            where u.Email == email || u.PhoneNumber == phoneNumber
            select u).First();

        InsertWallet(user.Id);
    }

    /// <summary>
    ///     Inserts a wallet of a random amount of money for a user.
    /// </summary>
    /// <param name="userId"></param>
    private void InsertWallet(int userId)
    {
        _context.Wallets.Add(new Wallet
        {
            UserId = userId,
            Balance = new Random().Next(25000, 99999)
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Updates a user's information.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="email"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="phoneNumber"></param>
    [HttpPut]
    [Route(nameof(UpdateUser) + "/{sessionKey}/{userId}/{email}/{firstName}/{lastName}/{phoneNumber}")]
    public void UpdateUser(int sessionKey, int userId, string email, string firstName, string lastName, string phoneNumber)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var user = (from u in _context.Users
            where u.Id == userId
            select u).FirstOrDefault();
        if (user is null) throw new Exception($"User with id {userId} does not exist.");

        user.Email = email;
        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all the groups a user is in.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     A list of groups.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetGroups) + "/{sessionKey}/{userId}")]
    public IEnumerable<Group> GetGroups(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        return from g in _context.Groups
            join utg in _context.UserToGroups
                on g.Id equals utg.GroupId
            where utg.UserId == userId
            select g;
    }

    /// <summary>
    ///     Creates a new group in the database.
    ///     Automatically adds the creator to the group as the owner.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupName"></param>
    [HttpPost]
    [Route(nameof(InsertGroup) + "/{sessionKey}/{userId}/{groupName}")]
    public void InsertGroup(int sessionKey, int userId, string groupName)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        _context.Groups.Add(new Group
        {
            Name = groupName,
            Date = DateTime.Now,
            Closed = false
        });
        _context.SaveChanges();
        _context.UserToGroups.Add(new UserToGroup
        {
            UserId = userId,
            GroupId = _context.Groups.OrderByDescending(g => g.Id).First().Id,
            IsOwner = true
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all payments of a user in a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A List of all payments of a user in a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserPayments) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<Payment> GetUserPayments(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupId = GetUserToGroupId(userId, groupId);
        return from payment in _context.Payments
            where payment.UserToGroupId == userToGroupId
            select payment;
    }

    /// <summary>
    ///     Gets all the current payments for a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    [HttpGet]
    [Route(nameof(GetGroupPayments) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<Payment> GetGroupPayments(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        return from payment in _context.Payments
            where GetUserToGroupIdsByGroupId(groupId).Contains(payment.UserToGroupId)
            select payment;
    }

    /// <summary>
    ///     Inserts a payment for a user in a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <param name="title"></param>
    /// <param name="amount"></param>
    [HttpPost]
    [Route(nameof(InsertPayment) + "/{sessionKey}/{userId}/{groupId}/{title}/{amount}")]
    public void InsertPayment(int sessionKey, int userId, int groupId, string title, double amount)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        if (amount < 1) throw new Exception("Amount must be greater than 0.");

        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0)
            throw new Exception($"User {userId} is not in group {groupId}.");

        _context.Payments.Add(new Payment
        {
            UserToGroupId = userToGroupId,
            Title = title,
            Amount = amount,
            Date = DateTime.Now
        });
        // Deduct from wallet
        var wallet = (from w in _context.Wallets
            where w.UserId == userId
            select w).FirstOrDefault();

        if (wallet is null)
            throw new Exception($"No wallet found for user {userId}");
        if (wallet.Balance < amount)
            throw new Exception($"Not enough money in wallet for user {userId}");

        wallet.Balance -= amount;
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all the invoices of a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     A list of invoices.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetInvoices) + "/{sessionKey}/{userId}")]
    public IEnumerable<Invoice> GetInvoices(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var user = (from u in _context.Users
            where u.Id == userId
            select u).FirstOrDefault();

        List<Invoice> invoices = new();
        var userToGroupIds = from utg in _context.UserToGroups where utg.UserId == userId select utg.Id;
        foreach (var userToGroupId in userToGroupIds)
        {
            _context = new DbWeshareContext();

            var group = (from g in _context.Groups
                join utg in _context.UserToGroups
                    on g.Id equals utg.GroupId
                where utg.Id == userToGroupId
                select g).FirstOrDefault();
            var wallet = (from w in _context.Wallets
                where w.UserId == user.Id
                select w).FirstOrDefault();
            var receipt = (from r in _context.Receipts
                where r.UserToGroupId == userToGroupId
                select r).FirstOrDefault();
            var payments = (from p in _context.Payments
                where p.UserToGroupId == userToGroupId
                select p).ToList();

            invoices.Add(new Invoice
            {
                User = user ?? new User(),
                Group = group ?? new Group(),
                Wallet = wallet ?? new Wallet(),
                Receipt = receipt ?? new Receipt(),
                Payments = payments
            });
        }

        return invoices;
    }

    /// <summary>
    ///     Gets all the friendships of a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     A List of all the friends of a user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetFriendships) + "/{sessionKey}/{userId}")]
    public IEnumerable<Friendship> GetFriendships(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var friendships = from f in _context.Friendships
            where f.UserId == userId
            select f;

        foreach (var friendship in friendships)
        {
            _context = new DbWeshareContext();
            friendship.Friend = (from u in _context.Users
                where u.Id == friendship.FriendId
                select u).FirstOrDefault() ?? new User();
        }

        return friendships;
    }
    
    /// <summary>
    ///     Gets a friend of a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    /// <returns>
    ///     A friend of a user of type User.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetFriend) + "/{sessionKey}/{userId}/{friendId}")]
    public IEnumerable<User> GetFriend(int sessionKey, int userId, int friendId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var friendship = (from f in _context.Friendships
            where f.UserId == userId && f.FriendId == friendId
            select f).FirstOrDefault();

        return from u in _context.Users
            where u.Id == friendship.FriendId
            select u;
    }
    
    /// <summary>
    ///     Returns the id of a friend by their email.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="email"></param>
    /// <returns>
    ///     The id of a friend.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetFriendId) + "/{sessionKey}/{userId}/{email}")]
    public IEnumerable<int> GetFriendId(int sessionKey, int userId, string email)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        return from u in _context.Users
            where u.Email == email
            select u.Id;
    }

    /// <summary>
    ///     Inserts a new friendship in the database.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    [HttpPost]
    [Route(nameof(InsertFriendship) + "/{sessionKey}/{userId}/{friendId}")]
    public void InsertFriendship(int sessionKey, int userId, int friendId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var friendship = from f in _context.Friendships
            where f.UserId == userId && f.FriendId == friendId
            select f;
        if (friendship.Any())
            throw new Exception($"Friendship between user {userId} and user {friendId} already exists.");

        _context.Friendships.Add(new Friendship
        {
            UserId = userId,
            FriendId = friendId
        });
        _context.SaveChanges();
    }


    /// <summary>
    ///     Removes a friendship from the database.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    [HttpDelete]
    [Route(nameof(DeleteFriendship) + "/{sessionKey}/{userId}/{friendId}")]
    public void DeleteFriendship(int sessionKey, int userId, int friendId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var friendship = (from f in _context.Friendships
            where f.UserId == userId && f.FriendId == friendId
            select f).FirstOrDefault();
        if (friendship is null)
            throw new Exception($"Friendship between user {userId} and user {friendId} does not exist.");

        _context.Friendships.Remove(friendship);
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all pending group invitations for a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     A list of group invitations.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetInvites) + "/{sessionKey}/{userId}")]
    public IEnumerable<Invite> GetInvites(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var invites = from i in _context.Invites
            where i.ReceiverId == userId
            select i;
        foreach (var invite in invites)
        {
            _context = new DbWeshareContext();
            // Set the Sender, Group and Receiver properties
            invite.Sender = (from u in _context.Users
                where u.Id == invite.SenderId
                select u).FirstOrDefault() ?? new User();
            invite.Group = (from g in _context.Groups
                where g.Id == invite.GroupId
                select g).FirstOrDefault() ?? new Group();
            invite.Receiver = (from u in _context.Users
                where u.Id == invite.ReceiverId
                select u).FirstOrDefault() ?? new User();
        }

        return invites;
    }

    /// <summary>
    ///     Returns a list of all invitable friends for a user in a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A list of all invitable friends for a user in a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetInvitableFriends) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<User> GetInvitableFriends(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId).ToList();

        var friends = from f in _context.Friendships
            where f.UserId == userId
            select f;

        List<User> invitableFriends = new();
        foreach (var friend in friends.ToList())
        {
            _context = new DbWeshareContext();

            if ((from utg in _context.UserToGroups
                    where utg.UserId == friend.FriendId && userToGroupIds.Contains(utg.Id)
                    select utg).FirstOrDefault() is not null)
                continue;

            invitableFriends.Add((from u in _context.Users
                where u.Id == friend.FriendId
                select u).FirstOrDefault() ?? new User());
        }

        return invitableFriends;
    }

    /// <summary>
    ///     Inserts a new group invite in the database.
    ///     The invite will not be sent if:
    ///     The user is already in the group...
    ///     The user has already been invited to the group...
    ///     The user is deactivated...
    ///     The group is closed...
    ///     The group has a value in the ToBePaid table...
    ///     The sender is not the owner of the group...
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="receiverId"></param>
    /// <param name="groupId"></param>
    [HttpPost]
    [Route(nameof(InsertInvite) + "/{sessionKey}/{userId}/{receiverId}/{groupId}")]
    public void InsertInvite(int sessionKey, int userId, int receiverId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        if (_context.UserToGroups.Any(u => u.UserId == receiverId && u.GroupId == groupId))
            throw new Exception($"User {receiverId} is already in group {groupId}.");
        if (_context.Invites.Any(i => i.SenderId == userId && i.ReceiverId == receiverId && i.GroupId == groupId))
            throw new Exception($"User {receiverId} has already been invited to group {groupId}.");
        if (_context.DeactivatedUsers.Any(u => u.UserId == receiverId))
            throw new Exception($"User {receiverId} is deactivated.");
        if (_context.Groups.Any(g => g.Id == groupId && g.Closed))
            throw new Exception($"Group {groupId} is closed.");
        if (_context.ToBePaids.Any(t => GetUserToGroupIdsByGroupId(groupId).Contains(t.UserToGroupId)))
            throw new Exception($"Group {groupId} has a value in the ToBePaid table.");
        if (!_context.UserToGroups.Any(u => u.UserId == userId && u.GroupId == groupId && u.IsOwner))
            throw new Exception($"User {userId} is not the owner of group {groupId}.");

        _context.Invites.Add(new Invite
        {
            SenderId = userId,
            ReceiverId = receiverId,
            GroupId = groupId
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Accepts a group invitation.
    ///     A user can only accept an invitation if:
    ///     - The user was invited to the group.
    ///     - The user is not already in the group.
    ///     - The group is not closed.
    ///     - The group is not marked ToBePaid.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut]
    [Route(nameof(AcceptInvite) + "/{sessionKey}/{userId}/{groupId}")]
    public void AcceptInvite(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var invite = (from i in _context.Invites
            where i.ReceiverId == userId && i.GroupId == groupId
            select i).FirstOrDefault();
        if (invite is null)
            throw new Exception($"User {userId} has not been invited to group {groupId}.");
        _context.Invites.Remove(invite);

        if (GetUserToGroupId(userId, groupId) is not 0)
            throw new Exception($"User {userId} is already in group {groupId}.");
        if (_context.Groups.Any(g => g.Id == groupId && g.Closed))
            throw new Exception($"Group {groupId} is closed.");
        if (_context.ToBePaids.Any(tbp => GetUserToGroupIdsByGroupId(groupId).Contains(tbp.UserToGroupId)))
            throw new Exception($"Group {groupId} has a value in the ToBePaid table.");

        _context.UserToGroups.Add(new UserToGroup
        {
            UserId = userId,
            GroupId = groupId,
            IsOwner = false
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Removes a group invite from the database.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpDelete]
    [Route(nameof(DeleteInvite) + "/{sessionKey}/{userId}/{groupId}")]
    public void DeleteInvite(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var invite = (from i in _context.Invites
            where i.ReceiverId == userId && i.GroupId == groupId
            select i).FirstOrDefault();
        if (invite is null) throw new Exception($"Invite for user {userId} to group {groupId} does not exist.");

        _context.Invites.Remove(invite);
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets the to be paids for a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A list of to be paids.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetGroupToBePaids) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<ToBePaid> GetGroupToBePaids(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        return from tbp in _context.ToBePaids
            where GetUserToGroupIdsByGroupId(groupId).Contains(tbp.UserToGroupId)
            select tbp;
    }

    /// <summary>
    ///     Get the to be paid for a user in a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A to be paid.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserToBePaid) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<ToBePaid> GetUserToBePaid(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupId = GetUserToGroupId(userId, groupId);
        return from tbp in _context.ToBePaids
            where tbp.UserToGroupId == userToGroupId
            select tbp;
    }

    /// <summary>
    ///     Inserts new values to ToBePaid table in the database.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPost]
    [Route(nameof(InsertToBePaid) + "/{sessionKey}/{userId}/{groupId}")]
    public void InsertToBePaid(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId).ToList();

        var isOwner = from utg in _context.UserToGroups
            where utg.UserId == userId && utg.GroupId == groupId
            select utg.IsOwner;
        if (!isOwner.First()) throw new Exception($"User {userId} is not the owner of group {groupId}.");

        var isToBePaid = from tbp in _context.ToBePaids
            where userToGroupIds.Contains(tbp.UserToGroupId)
            select tbp;
        if (isToBePaid.Any()) throw new Exception($"Group {groupId} is already marked ToBePaid.");

        _context.ToBePaids.Add(new ToBePaid
        {
            UserToGroupId = GetUserToGroupId(userId, groupId),
            Approved = true,
            Date = DateTime.Now
        });

        userToGroupIds.Remove(GetUserToGroupId(userId, groupId));
        _context.ToBePaids.AddRange(userToGroupIds.Select(userToGroupId => new ToBePaid
        {
            UserToGroupId = userToGroupId,
            Approved = false,
            Date = null
        }));

        _context.SaveChanges();

        // Remove all the invites for the group.
        foreach (var invite in _context.Invites.Where(i => i.GroupId == groupId))
        {
            _context.Invites.Remove(invite);
            _context.SaveChanges();
        }
    }

    /// <summary>
    ///     Approves a ToBePaid value in the database.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut]
    [Route(nameof(ApproveToBePaid) + "/{sessionKey}/{userId}/{groupId}")]
    public void ApproveToBePaid(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0)
            throw new Exception($"User {userId} is not in group {groupId}.");

        var toBePaid = _context.ToBePaids.FirstOrDefault(t => t.UserToGroupId == userToGroupId);
        if (toBePaid is null)
            throw new Exception($"User {userId} is not marked ToBePaid in group {groupId}.");

        if (toBePaid.Approved)
            throw new Exception($"User {userId} already approved ToBePaid in group {groupId}.");

        toBePaid.Approved = true;
        toBePaid.Date = DateTime.Now;
        _context.SaveChanges();
        // if all ToBePaid values are approved for the group
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId);
        if (_context.ToBePaids.Where(t => userToGroupIds.Contains(t.UserToGroupId)).All(t => t.Approved))
            InsertReceipts(groupId);
    }

    /// <summary>
    ///     Inserts receipts for a group.
    /// </summary>
    /// <param name="groupId"></param>
    private void InsertReceipts(int groupId)
    {
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId).ToList();
        // Get the total amount of money that the group has spent.
        var totalAmount = _context.Payments.Where(p => userToGroupIds.Contains(p.UserToGroupId)).Sum(p => p.Amount);
        // Get the amount that each user should pay.
        var amountPerUser = totalAmount / userToGroupIds.Count;
        // Get every payment for each user even if the user didn't pay anything.
        var payments = _context.Payments.Where(p => userToGroupIds.Contains(p.UserToGroupId)).ToList();
        // If payments does not contain a payment for a user, add it with amount 0.
        foreach (var userToGroupId in userToGroupIds.Where(userToGroupId =>
                     payments.All(p => p.UserToGroupId != userToGroupId)))
            payments.Add(new Payment
            {
                Amount = 0,
                UserToGroupId = userToGroupId
            });

        // Get the amount that each user has paid.
        foreach (var utg in userToGroupIds)
        {
            var totalPaid = payments.Where(p => p.UserToGroupId == utg).Sum(p => p.Amount);
            _context.Receipts.Add(new Receipt
            {
                UserToGroupId = utg,
                Amount = totalPaid - amountPerUser,
                Fulfilled = false
            });
        }

        _context.SaveChanges();
    }

    /// <summary>
    ///     Removes a group's ToBePaid values from the database.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpDelete]
    [Route(nameof(DeleteToBePaid) + "/{sessionKey}/{userId}/{groupId}")]
    public void DeleteToBePaid(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0)
            throw new Exception($"User {userId} is not in group {groupId}.");

        // If the user is not the owner of the group
        if (!_context.UserToGroups.Any(u => u.UserId == userId && u.GroupId == groupId && u.IsOwner))
            throw new Exception($"User {userId} is not the owner of group {groupId}.");

        // If all ToBePaid values are approved for the group
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId);
        if (_context.ToBePaids.Where(t => userToGroupIds.Contains(t.UserToGroupId)).All(t => t.Approved))
            throw new Exception($"Group {groupId} is already approved ToBePaid.");

        userToGroupIds.ToList().ForEach(utg =>
            _context.ToBePaids.Remove(_context.ToBePaids.First(t => t.UserToGroupId == utg)));
        _context.SaveChanges();
    }


    /// <summary>
    ///     Gets the receipt for a user in a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     The receipt for a user in a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetReceipt) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<Receipt> GetReceipt(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupId = GetUserToGroupId(userId, groupId);
        return from receipt in _context.Receipts
            where receipt.UserToGroupId == userToGroupId
            select receipt;
    }

    /// <summary>
    ///     Fulfills a receipt by deducting the amount from the user's balance and marking the receipt as fulfilled.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut]
    [Route(nameof(FulfillReceipt) + "/{sessionKey}/{userId}/{groupId}")]
    public void FulfillReceipt(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId is 0)
            throw new Exception($"User {userId} is not in group {groupId}.");

        var receipt = (from r in _context.Receipts
            where r.UserToGroupId == userToGroupId
            select r).FirstOrDefault();
        if (receipt is null)
            throw new Exception($"User {userId} does not have a receipt in group {groupId}.");

        var wallet = (from w in _context.Wallets
            where w.UserId == userId
            select w).FirstOrDefault();
        if (wallet is null)
            throw new Exception($"User {userId} does not have a wallet.");

        if (wallet.Balance < receipt.Amount)
            throw new Exception($"User {userId} does not have enough money in their wallet.");

        // += because if someone owes money, the amount is negative.
        wallet.Balance += receipt.Amount;
        receipt.Date = DateTime.Now;
        receipt.Fulfilled = true;
        _context.SaveChanges();

        // if all receipts are fulfilled for the group, close the group.
        var groupReceipts = from r in _context.Receipts
            where r.UserToGroupId == userToGroupId
            select r;
        if (!groupReceipts.All(r => r.Fulfilled)) return;

        var group = (from g in _context.Groups
            where g.Id == groupId
            select g).First();
        group.Closed = true;
        _context.SaveChanges();
    }

    /// <summary>
    ///     Removes a user from a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <param name="userToRemoveId"></param>
    [HttpDelete]
    [Route(nameof(DeleteUserFromGroup) + "/{sessionKey}/{userId}/{groupId}/{userToRemoveId}")]
    public void DeleteUserFromGroup(int sessionKey, int userId, int groupId, int userToRemoveId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupId = GetUserToGroupId(userToRemoveId, groupId);
        if (userToGroupId is 0)
            throw new Exception($"User {userToRemoveId} is not in group {groupId}.");

        if (_context.Groups.Any(g => g.Id == groupId && g.Closed))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because the group is closed.");

        if (!_context.UserToGroups.Any(utg => utg.UserId == userId && utg.GroupId == groupId && utg.IsOwner))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because user {userId} is not the owner.");

        if (_context.Receipts.Any(r => r.UserToGroupId == userToGroupId))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because they have a receipt.");

        if (_context.ToBePaids.Any(t => t.UserToGroupId == userToGroupId))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because the group is marked as to be paid.");

        if (_context.Payments.Any(p => p.UserToGroupId == userToGroupId))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because they have a payment.");

        var userToRemove = from utg in _context.UserToGroups
            where utg.Id == userToGroupId
            select utg;
        _context.UserToGroups.Remove(userToRemove.First());
        _context.SaveChanges();
    }


    /// <summary>
    ///     Gets the balance of a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     The balance of a user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetBalance) + "/{sessionKey}/{userId}")]
    public IEnumerable<double> GetBalance(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        return from w in _context.Wallets
            where w.UserId == userId
            select w.Balance;
    }

    /// <summary>
    ///     Activates a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    [HttpDelete]
    [Route(nameof(ActivateUser) + "/{sessionKey}/{userId}")]
    public void ActivateUser(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var user = (from du in _context.DeactivatedUsers
            where du.UserId == userId
            select du).FirstOrDefault();

        if (user is null)
            throw new Exception($"User {userId} is not deactivated.");

        if (user.ByAdmin)
            throw new Exception($"User {userId} is deactivated by an admin and cannot activate themselves.");

        _context.DeactivatedUsers.Remove(user);
        _context.SaveChanges();
    }

    /// <summary>
    ///     Deactivates a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    [HttpPost]
    [Route(nameof(DeactivateUser) + "/{sessionKey}/{userId}")]
    public void DeactivateUser(int sessionKey, int userId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        if (_context.DeactivatedUsers.Any(du => du.UserId == userId))
            throw new Exception($"User {userId} is already deactivated.");

        _context.DeactivatedUsers.Add(new DeactivatedUser
        {
            UserId = userId,
            ByAdmin = false
        });
        _context.SaveChanges();
    }


    /// <summary>
    ///     Gets the total amount of money spent by a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     The total amount of money spent by a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetTotalSpent) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<double> GetTotalSpent(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        yield return (from p in _context.Payments
            where GetUserToGroupIdsByGroupId(groupId).Contains(p.UserToGroupId)
            select p.Amount).Sum();
    }

    /// <summary>
    ///     Gets the fair share of a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     The fair share of a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetFairShare) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<double> GetFairShare(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var totalSpent = (from p in _context.Payments
            where GetUserToGroupIdsByGroupId(groupId).Contains(p.UserToGroupId)
            select p.Amount).Sum();
        var numberOfUsers = GetUserToGroupIdsByGroupId(groupId).Count();
        yield return totalSpent / numberOfUsers;
    }

    /// <summary>
    ///     Checks if a group is marked to be paid.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A boolean indicating if a group is marked to be paid.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsMarkedToBePaid) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<bool> IsMarkedToBePaid(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        yield return _context.ToBePaids.Any(tbp => GetUserToGroupIdsByGroupId(groupId).Contains(tbp.UserToGroupId) && !tbp.Approved);
    }

    /// <summary>
    ///     Checks if all of the users in a group have approved the payment.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A boolean indicating if a group is approved to be paid.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsApprovedToBePaid) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<bool> IsApprovedToBePaid(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId);
        yield return _context.ToBePaids.Count(tbp => userToGroupIds.Contains(tbp.UserToGroupId) && tbp.Approved) == userToGroupIds.Count();
    }

    /// <summary>
    ///     Checks if a user has fulfilled their receipt in a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A boolean indicating if a user has fulfilled their receipt.
    /// </returns>
    [HttpGet]
    [Route(nameof(HasFulfilledReceipt) + "/{sessionKey}/{userId}/{groupId}")]
    public IEnumerable<bool> HasFulfilledReceipt(int sessionKey, int userId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, userId).First()) throw new Exception("Invalid session key");
        
        yield return _context.Receipts.Any(r => r.UserToGroupId == GetUserToGroupId(userId, groupId) && r.Fulfilled);
    }
}
