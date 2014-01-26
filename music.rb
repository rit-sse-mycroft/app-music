require 'mycroft'
require 'hallon'
require 'highline/import'

class Music < Mycroft::Client

  attr_accessor :verified

  def initialize(host, port)
    @key = ''
    @cert = ''
    @manifest = './app.json'
    @verified = false
    @dependencies = {}
    @sent_grammar = false
    @status = 'down'
    begin
      app_key = IO::read('./spotify_appkey.key')
      has_app_key = true
    rescue
      has_app_key = false
    end
    if has_app_key
      username = ask("Enter your Spotify username:  ")
      password = ask("Enter your Spotify password:  ") { |q| q.echo = false }
      @session = Hallon::Session.initialize(app_key) do
        on(:log_message) do |message|
          puts "[LOG] #{message}"
        end

        on(:credentials_blob_updated) do |blob|
          puts "[BLOB] #{blob}"
        end

        on(:connection_error) do |error|
          Hallon::Error.maybe_raise(error)
        end

        on(:logged_out) do
          abort "[FAIL] Logged out!"
        end
      end
      @session.login!(username, password)
      super
    end
  end

  def connect
    # Your code here
  end

  def on_data(data)
    if data[:type] == 'APP_DEPENDENCY'
      update_dependencies(parsed[:data])
      puts "Current status of dependencies"
      puts @dependencies
      if stt_up? and not @sent_grammar
        data = {grammar: { name: 'music', xml: File.read('./grammar.xml')}}
        query('stt', 'load_grammar', data)
        @sent_grammar = true
      elsif not stt_up? and @sent_grammar
        @sent_grammar = false
      end
      if stt_up? and speakers_up? and @status == 'down'
        up
        @status = 'up'
      elsif((not stt_up? or not speakers_up?) and status == 'up')
        down
        @status = 'down'
      end
    elsif data[:type] == 'MSG_BROADCAST'

    end
  end

  def on_end
    # Your code here
  end

  def speakers_up?
    not @dependencies['audioOutput'].nil? and @dependences['audioOutput']['speakers'] == 'up'
  end

  def stt_up?
    not @dependencies['stt'].nil? and @dependences['stt']['stt1'] == 'up'
  end
end

Mycroft.start(Music)
