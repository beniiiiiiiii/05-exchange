using System;
using System.Collections.Generic;
using System.Text;

namespace Solution.Common.Constants.Errors;

public static partial class Errors
{
    public static class User
    {
        public static Error NotFound => Error.NotFound(
            code: "User.NotFound",
            description: "User not found."
        );

        public static Error EmailAlreadyExists => Error.Conflict(
            code: "User.EmailAlreadyExists",
            description: "A user with this email already exists."
        );

        public static Error CreationFailed => Error.Failure(
            code: "User.CreationFailed",
            description: "Failed to create user."
        );

        public static Error DeletionFailed => Error.Failure(
            code: "User.DeletionFailed",
            description: "Failed to delete user."
        );

        public static Error CannotDeleteSelf => Error.Validation(
            code: "User.CannotDeleteSelf",
            description: "Users cannot delete themselves."
        );

        public static Error PasswordResetFailed => Error.Failure(
            code: "User.PasswordResetFailed",
            description: "Failed to reset user password."
        );

        public static Error Unauthorized => Error.Unauthorized(
            code: "User.Unauthorized",
            description: "Unauthorized access to user resource."
        );
    }
}
