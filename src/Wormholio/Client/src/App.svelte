<script lang="ts">
    interface sig {
        id: string,
        type: string,
        group: string,
        name: string,
        signal: number,
        distance: string
    }

    let signatures: sig[] = [];

    const fetchCharacters = (async () => {
        const response = await fetch('/me');
        return await response.json();
    })();

    onpaste = async (event) => {
        event.preventDefault();
        if (event.clipboardData == null)
            return;

        const text = event.clipboardData.getData('text');
        const response = await fetch('/signatures/paste', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({system: 1234, clipboard: text}),
        });

        let sigs: sig[] = [];
        Object.assign(sigs, await response.json());
        signatures = sigs;
    };
</script>

<main>
    <span>
        <a href="/auth/login"><button>Log In</button></a>
        <a href="/auth/logout"><button>Log Out</button></a>
    </span>
    {#await fetchCharacters}
        <p>Fetching characters...</p>
    {:then data}
        <h2>My Characters</h2>
        <ul>
            {#each data as name}
                <li>
                    {name}
                </li>
            {/each}
        </ul>
    {:catch error}
        <p style="color: red">{error.message}</p>
    {/await}
    <a href="/auth/login">
        <button>Add Character</button>
    </a>
    <ul>
        {#each signatures as sig}
            <li>{sig.id} {sig.name} ({sig.group}) {sig.signal}% - {sig.distance}</li>
        {/each}
    </ul>
</main>

<style>
    .logo {
        height: 6em;
        padding: 1.5em;
        will-change: filter;
        transition: filter 300ms;
    }

    .logo:hover {
        filter: drop-shadow(0 0 2em #646cffaa);
    }

    .logo.svelte:hover {
        filter: drop-shadow(0 0 2em #ff3e00aa);
    }

    .read-the-docs {
        color: #888;
    }
</style>
