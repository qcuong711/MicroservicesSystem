import { auth } from "@/auth";
import { handleSignIn, handleSignOut } from "@/app/actions";

export default async function LoginButton() {
  const session = await auth();
  const user = session?.user;

  return (
    <div className="flex items-center">
      {user ? (
        <form action={handleSignOut}>
          <button
            type="submit"
            className="whitespace-nowrap rounded-md bg-gray-900 px-4 py-2 text-sm font-medium text-gray-50 shadow transition-colors hover:bg-gray-900/90 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-gray-950 disabled:pointer-events-none disabled:opacity-50 dark:bg-gray-50 dark:text-gray-900 dark:hover:bg-gray-50/90 dark:focus-visible:ring-gray-300"
          >
            Sign Out
          </button>
        </form>
      ) : (
        <form action={handleSignIn}>
          <button
            type="submit"
            className="whitespace-nowrap rounded-md bg-gray-900 px-4 py-2 text-sm font-medium text-gray-50 shadow transition-colors hover:bg-gray-900/90 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-gray-950 disabled:pointer-events-none disabled:opacity-50 dark:bg-gray-50 dark:text-gray-900 dark:hover:bg-gray-50/90 dark:focus-visible:ring-gray-300"
          >
            Sign In
          </button>
        </form>
      )}
    </div>
  );
} 