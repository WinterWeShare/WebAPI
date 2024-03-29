﻿namespace WebAPI.Models.EntityFramework;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public virtual ICollection<DeactivatedUser> DeactivatedUsers { get; } = new List<DeactivatedUser>();

    public virtual ICollection<Friendship> FriendshipFriends { get; } = new List<Friendship>();

    public virtual ICollection<Friendship> FriendshipUsers { get; } = new List<Friendship>();

    public virtual ICollection<Invite> InviteReceivers { get; } = new List<Invite>();

    public virtual ICollection<Invite> InviteSenders { get; } = new List<Invite>();

    public virtual ICollection<UserPassword> UserPasswords { get; } = new List<UserPassword>();

    public virtual ICollection<UserRecoveryCode> UserRecoveryCodes { get; } = new List<UserRecoveryCode>();

    public virtual ICollection<UserSession> UserSessions { get; } = new List<UserSession>();

    public virtual ICollection<UserToGroup> UserToGroups { get; } = new List<UserToGroup>();

    public virtual ICollection<Wallet> Wallets { get; } = new List<Wallet>();
}
